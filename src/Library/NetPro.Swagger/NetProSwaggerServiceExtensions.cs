﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.OpenApi.Models;
using NetPro.TypeFinder;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetPro.Swagger
{
    public static class NetProSwaggerServiceExtensions
    {
        public static IServiceCollection AddNetProSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            ILogger logger = null;
            if (loggerFactory != null)
            {
                logger = loggerFactory.CreateLogger($"{nameof(NetProSwaggerServiceExtensions)}");
            }
            services.Configure<SwaggerOption>(configuration.GetSection(nameof(SwaggerOption)));
            var swaggerOption = services.BuildServiceProvider().GetService<IOptions<SwaggerOption>>().Value;
            //var swaggerOption = configuration.GetSection(nameof(SwaggerOption)).Get<SwaggerOption>();
            if (!swaggerOption.Enabled)
            {
                logger?.LogInformation($"NetPro Swagger 已关闭");
                return services;
            }
            else
            {
                logger?.LogInformation($"NetPro Swagger 已启用");
            }
            services.AddSingleton(swaggerOption);
            services.AddFileProcessService();

            //services
            //    .Configure<OpenApiInfo>(configuration.GetSection("SwaggerOption"));

            //var info = services.BuildServiceProvider().GetService<IOptions<OpenApiInfo>>().Value;

            services.AddSwaggerGen(c =>
            {
                c.DescribeAllParametersInCamelCase();//请求参数转小写
                c.OperationFilter<SwaggerFileUploadFilter>();//add file fifter component
                c.OperationFilter<SwaggerDefaultValueFilter>();//add webapi  default value of parameter
                c.OperationFilter<CustomerHeaderParameter>();//add default header
                c.OperationFilter<CustomerQueryParameter>();//add default query

                var securityRequirement = new OpenApiSecurityRequirement();
                securityRequirement.Add(new OpenApiSecurityScheme { Name = "Bearer" }, new string[] { });
                c.AddSecurityRequirement(securityRequirement);

                //batch find xml file of swagger
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;//get app root path
                List<string> xmlComments = GetXmlComments();
                xmlComments.ForEach(r =>
                {
                    string filePath = Path.Combine(basePath, r);
                    if (File.Exists(filePath))
                    {
                        c.IncludeXmlComments(filePath);
                    }
                });

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = swaggerOption.Title,
                    Version = swaggerOption.Version,
                    Description = swaggerOption.Description,
                    //TermsOfService = "None",
                    Contact = swaggerOption.Contact,// new OpenApiContact { Email = "Email", Name = "Name", Url = new Uri("http://www.github.com") },
                    License = swaggerOption.License,//new OpenApiLicense { Url = new Uri("http://www.github.com"), Name = "LicenseName" },
                });
                c.IgnoreObsoleteActions();
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Authority certification(The data is transferred in the request header) structure of the parameters : \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });
            });
            services.AddSwaggerGenNewtonsoftSupport();

            return services;
        }

        /// <summary>
        /// 所有xml默认当作swagger文档注入swagger
        /// </summary>
        /// <returns></returns>
        private static List<string> GetXmlComments()
        {
            //var pattern = $"^{netProOption.ProjectPrefix}.*({netProOption.ProjectSuffix}|Domain)$";
            //List<string> assemblyNames = ReflectionHelper.GetAssemblyNames(pattern);
            List<string> assemblyNames = AppDomain.CurrentDomain.GetAssemblies().Select(s => s.GetName().Name).ToList();
            List<string> xmlFiles = new List<string>();
            assemblyNames.ForEach(r =>
            {
                string fileName = $"{r}.xml";
                xmlFiles.Add(fileName);
            });
            return xmlFiles;
        }
    }

    public static class NetProSwaggerMiddlewareExtensions
    {
        public static IApplicationBuilder UseNetProSwagger(
         this IApplicationBuilder application)
        {
            var configuration = application.ApplicationServices.GetService<IConfiguration>();
            var swaggerOption = application.ApplicationServices.GetService<IOptions<SwaggerOption>>().Value;

            if (swaggerOption.Enabled)
            {
                var basePath = swaggerOption.ServerPrefix;
                application.UseSwagger(c =>
                {
                    c.RouteTemplate = "docs/{documentName}/docs.json";//使中间件服务生成Swagger作为JSON端点
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Info.Description = httpReq.Path);//请求过滤处理
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                    {
                        var refererPath = httpReq.Headers.Where(s => "referer".Equals(s.Key.ToLower())).Select(s => s.Value);
                        if (refererPath.Any())
                        {
                            var refererPathUri = new Uri($"{refererPath.First()}");
                            swaggerDoc.Servers.Add(new OpenApiServer { Url = $"{httpReq.Scheme}://{refererPathUri.Host}:{refererPathUri.Port}/{basePath}" });
                        }

                        swaggerDoc.Servers.Add(new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/{basePath}" });
                    });
                });

                application.UseSwaggerUI(c =>
                {
                    c.DocExpansion(DocExpansion.List);
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.MaxDisplayedTags(5);
                    c.ShowExtensions();
                    c.ShowCommonExtensions();
                    c.EnableValidator();
                    //c.SupportedSubmitMethods(SubmitMethod.Get, SubmitMethod.Head);

                    c.RoutePrefix = $"{swaggerOption.RoutePrefix}";//设置文档首页根路径
                    var prefix = !string.IsNullOrEmpty(basePath) ? $"/{basePath}/" : "/";
                    c.SwaggerEndpoint($"{prefix}docs/v1/docs.json", $"{swaggerOption.Title}");//此处配置要和UseSwagger的RouteTemplate匹配
                    c.SwaggerEndpoint("https://petstore.swagger.io/v2/swagger.json", "petstore.swagger");//远程swagger示例   

                    #region
                    typeof(NetProSwaggerMiddlewareExtensions).GetTypeInfo().Assembly.GetManifestResourceStream("NetPro.Swagger.SwaggerProfiler.html");
                    #endregion
                    if (swaggerOption.IsDarkTheme)
                        c.IndexStream = () => typeof(NetProSwaggerMiddlewareExtensions).GetTypeInfo().Assembly.GetManifestResourceStream("NetPro.Swagger.IndexDark.html");
                    else
                        c.IndexStream = () => typeof(NetProSwaggerMiddlewareExtensions).GetTypeInfo().Assembly.GetManifestResourceStream("NetPro.Swagger.Index.html");

                });
            }

            return application;
        }
    }

    public class NetProSwaggerMiddleware
    {
        private readonly RequestDelegate _next;

        public NetProSwaggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {

            await _next(context);
            return;
        }
    }
}
