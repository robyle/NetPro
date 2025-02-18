﻿using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetPro.Swagger
{
    public class SwaggerOption
    {
        /// <summary>
        /// 是否启用swagger
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 是否暗黑主题
        /// </summary>
        public bool IsDarkTheme { get; set; } = false;
        /// <summary>
        /// Swagge路由前缀
        /// </summary>
        public string RoutePrefix { get; set; } = "swagger";

        /// <summary>
        /// 终结点地址前缀
        /// </summary>
        /// <remarks>
        /// 配置/xx 实际描述终结点地址将为：/xx/docs/v1/docs.json
        /// </remarks>
        /// <example>
        /// "ServerPrefix": "/xx"     
        /// </example>
        public string ServerPrefix { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// version
        /// </summary>
        public string Version { get; set; }
        public string TermsOfService { get; set; }
        public OpenApiContact Contact { get; set; }
        public OpenApiLicense License { get; set; }

        /// <summary>
        /// 全局头参数
        /// </summary>
        public Header[] Headers { get; set; }

        /// <summary>
        /// 全局Query参数
        /// </summary>
        public Query[] Query { get; set; }
    }

    public class Header
    {
        /// <summary>
        /// 头参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; }
    }

    public class Query
    {
        /// <summary>
        /// Query参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Query参数描述信息
        /// </summary>
        public string Description { get; set; }
    }

}
