﻿using NetPro.Core.Configuration;
using NetPro.Web.Core.Helpers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace NetPro.Web.Core.Filters
{
    /// <summary>
    /// 请求数据监控
    /// </summary>
    public class ReqeustBodyFilter : IAsyncActionFilter
    {
        private readonly ILogger _logger;
        readonly NetProOption _config;
        readonly IWebHelper _webHelper;

        public ReqeustBodyFilter(ILogger logger, NetProOption config, IWebHelper webHelper)
        {
            _logger = logger;
            _config = config;
            _webHelper = webHelper;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            try
            {
                var isDebug = _config.IsDebug;
                if (isDebug)
                {
                    string requestBodyText = string.Empty;
                    var request = context.HttpContext.Request;
                    var method = request.Method.ToUpper();
                    var url = HttpUtility.UrlDecode(UriHelper.GetDisplayUrl(request));
                    var macName = Environment.MachineName;
                    var requestIp = _webHelper.GetCurrentIpAddress();
                    var bodyText = ActionFilterHelper.GetRequestBodyText(request);
                    if (Regex.IsMatch(bodyText, "(\\d+?,)+"))
                    {
                        var index = url.IndexOf('?');
                        if (index > -1)
                            url = url.Substring(0, index);
                        string filePath = Path.Combine("C:", "BadUrls");
                        await WriteTxt(filePath, $"{request.Host.Port}.txt", url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "【ReqeustBodyFilter】error：" + ex.Message);
            }
        }

        private async Task WriteTxt(string directoryPath, string fileName, string input)
        {
            try
            {
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                string filePath = Path.Combine(directoryPath, fileName);
                if (!File.Exists(filePath))
                {
                    using (var fs = File.Create(filePath))
                    {

                    }
                }

                var allLines = File.ReadAllLines(filePath);
                if (allLines.Contains(input.Trim())) return;
                using (var fs = File.Open(filePath, FileMode.Append, FileAccess.Write))
                {
                    var buffer = Encoding.UTF8.GetBytes(input + Environment.NewLine);
                    await fs.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "【WriteTxt】error：" + ex.Message);
            }
            
        }
    }
}
