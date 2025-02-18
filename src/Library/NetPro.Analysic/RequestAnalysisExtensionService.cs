﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPro.CsRedis;
using System;
namespace NetPro.Analysic
{
    /// <summary>
    /// 
    /// </summary>
    public static class RequestAnalysisExtensionService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRequestAnalysic(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            try
            {
                var option = configuration.GetSection(nameof(RequestAnalysisOption)).Get<RequestAnalysisOption>();
                if (option == null || !option.Enabled)
                    return services;
                services.AddHttpContextAccessor();
                services.AddSingleton(option);
                var redisCacheOption = configuration.GetSection(nameof(RedisCacheOption)).Get<RedisCacheOption>();
                if (redisCacheOption != null && redisCacheOption.Enabled)
                {
                    //新增redis缓存注入
                    services.AddCsRedis<NetPro.CsRedis.SystemTextJsonSerializer>(configuration);
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException("流量分析依赖NetPro.CsRedis组件,请检查是否注入服务并遗漏RedisCacheOption配置节点", ex);
            }

            return services;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RequestAnalysicBehaviorOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public Func<bool, HttpContext> RequestAnalysicStateResponseFactory { get; set; }
    }
}
