﻿using GgcmsCSharp.ApiCtrls;
using GgcmsCSharp.Models;
using System.Configuration;
using System.Web.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Web.Routing;
using System.Text.RegularExpressions;
using GgcmsCSharp.Utils;
using System.Web;
using System;

namespace GgcmsCSharp.Controllers
{
    public enum PageType
    {
        home,
        category,
        article
    }
    public class HomeController : Controller
    {
        // GET: Home
        private DataHelper dataHelper;
        private Dictionary<string, string> sysConfigs;
        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            dataHelper = new DataHelper();
            sysConfigs = dataHelper.SysConfigs();
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            ViewBag.Title = sysConfigs["cfg_webname"];
            ViewBag.categories = dataHelper.Categories();
            ViewBag.sysConfig = sysConfigs;
            ViewBag.dataHelper = dataHelper;
            ViewBag.topId = -1;
            ViewBag.AppVirtualPath = HttpRuntime.AppDomainAppVirtualPath;
            ViewBag.Prefix = dataHelper.Prefix;
            CacheHelper.SetPages(Request.Url.AbsolutePath);
        }
        [OutputCache(CacheProfile = "IndexCache")]
        public ActionResult Index()
        {
            HttpClient client = new HttpClient();
            ViewBag.Title = sysConfigs.ContainsKey("cfg_indexname")?sysConfigs["cfg_indexname"] :"";
            ViewBag.Description = sysConfigs.ContainsKey("cfg_description") ? sysConfigs["cfg_description"] : ""; 
            ViewBag.Keywords = sysConfigs.ContainsKey("cfg_keywords") ? sysConfigs["cfg_keywords"] : ""; 
            var pt = getTemplate();
            ViewBag.pageTmpl = pt;
            ViewBag.topId = 0;
            ViewBag.PageType = 0;
            return View(pt.templateUrl);

        }
        [OutputCache(CacheProfile = "ListCache")]
        public ActionResult Category(string id, int page = 1)
        {

            GgcmsCategories category;
            if (Tools.IsInt(id))
            {
                int intid = Tools.parseInt(id);
                category = dataHelper.Categories(intid);
            }
            else
            {
                category = dataHelper.Categories(id);
            }
            if (category == null)
            {
                return HttpNotFound();
            }
            if (!string.IsNullOrWhiteSpace(category.Content))
            {
                category.Content = ContentKeys(category.Content);
            }
            ViewBag.Description = category.Description;
            ViewBag.Keywords = category.Keywords;

            Pagination pagination = new Pagination();
            pagination.page = page;
            pagination.baseLink = dataHelper.Prefix + "/Category/" + id + "/{page}";
            pagination.pageSize = Tools.parseInt(sysConfigs["cfg_page_size"], 10);
            if (category.PageSize > 0)
            {
                pagination.pageSize = category.PageSize;
            }
            //跳转到错误页
            if (category == null)
            {

            }
            int[] ids = GgcmsCategories.GetCategoryIds(category);
            ViewBag.pagination = pagination;
            ViewBag.Title = category.CategoryName;
            ViewBag.category = category;
            var result= dataHelper.Articles(ids, pagination);
            ViewBag.articles = result.Records;
            pagination.setMaxSize(Convert.ToInt32( result.Count));

            ViewBag.topId = dataHelper.GetCategoryTopId(category);
            ViewBag.PageType = 1;
            var pt = getTemplate(PageType.category, category);
            ViewBag.pageTmpl = pt;
            return View(pt.templateUrl);
        }
        [OutputCache(CacheProfile = "ViewCache")]
        public ActionResult Article(int id, int page = 1)
        {
            GgcmsArticles article = dataHelper.Article(id, page);
            //跳转到错误页
            if (article == null)
            {

            }
            Pagination pagination = new Pagination();
            pagination.page = page;
            pagination.baseLink = dataHelper.Prefix + "/Article/" + id + "/{page}";
            pagination.pageSize = 1;
            pagination.setMaxSize(article.pagesCount);
            ViewBag.pagination = pagination;
            article.Content = ContentKeys(article.Content);
            GgcmsCategories category = dataHelper.Categories(article.Category_Id);
            ViewBag.Description = article.Description;
            ViewBag.Keywords = article.Keywords;
            ViewBag.Title = article.Title;
            ViewBag.article = article;
            ViewBag.category = category;
            ViewBag.topId = dataHelper.GetCategoryTopId(category);
            var pt = getTemplate(PageType.article, article);
            ViewBag.pageTmpl = pt;
            ViewBag.PageType = 2;
            return View(pt.templateUrl);
        }
        private PageTemplate getTemplate(PageType ptype = PageType.home, dynamic info = null)
        {
            string cfgStyle = sysConfigs["cfg_default_style"];
            string templateDir = ConfigurationManager.AppSettings["TemplateDir"].ToString();
            templateDir = "~/Views/" + templateDir + "/" + cfgStyle;
            string staticDir = ConfigurationManager.AppSettings["StaticDir"].ToString();
            string styleDir = ConfigurationManager.AppSettings["StyleDir"].ToString();
            string stylePath = "/" + staticDir + "/" + styleDir + "/" + cfgStyle;
            bool isMobile = getIsMobile();
            GgcmsCategories category;
            string tmplName;
            switch (ptype)
            {
                case PageType.home:
                    string home = sysConfigs["cfg_template_home"];
                    string turl = templateDir + "/" + home;
                    if (isMobile && !string.IsNullOrEmpty(sysConfigs["cfg_template_m_home"].Trim()))
                    {
                        turl = templateDir + "/" + sysConfigs["cfg_template_m_home"];
                    }
                    return new PageTemplate
                    {
                        templatePath = templateDir,
                        templateUrl = turl,
                        stylePath = stylePath
                    };
                case PageType.category:
                    tmplName = sysConfigs["cfg_template_list"];
                    category = info as GgcmsCategories;
                    string style = category.StyleName ?? cfgStyle??"";
                    if (!string.IsNullOrWhiteSpace(style))
                    {
                        templateDir = "~/Views/" + ConfigurationManager.AppSettings["TemplateDir"].ToString() + "/" + style;
                        stylePath = "/" + staticDir + "/" + styleDir + "/" + style;
                        if (isMobile && !string.IsNullOrWhiteSpace(category.MobileTmplName.Trim()))
                        {
                            tmplName = category.MobileTmplName;
                        }
                        else if (!string.IsNullOrWhiteSpace(category.TmplName))
                        {
                            tmplName = category.TmplName;
                        }

                    }
                    return new PageTemplate
                    {
                        templatePath = templateDir,
                        templateUrl = templateDir + "/" + tmplName,
                        stylePath = stylePath
                    };
                case PageType.article:
                    tmplName = sysConfigs["cfg_template_view"];
                    GgcmsArticles article = info as GgcmsArticles;
                    category = dataHelper.Categories(article.Category_Id);
                    if (!string.IsNullOrEmpty(article.StyleName.Trim()))
                    {
                        templateDir = "~/Views/" + ConfigurationManager.AppSettings["TemplateDir"].ToString() + "/" + article.StyleName;
                        stylePath = "/" + staticDir + "/" + styleDir + "/" + article.StyleName;
                        if (isMobile && !string.IsNullOrEmpty(article.MobileTmplName.Trim()))
                        {
                            tmplName = article.MobileTmplName;
                        }
                        else if (!string.IsNullOrEmpty(article.TmplName.Trim()))
                        {
                            tmplName = article.TmplName;
                        }

                    }
                    else if (!string.IsNullOrEmpty(category.StyleName.Trim()))
                    {
                        templateDir = "~/Views/" + ConfigurationManager.AppSettings["TemplateDir"].ToString() + "/" + category.StyleName;
                        stylePath = "/" + staticDir + "/" + styleDir + "/" + category.StyleName;
                        if (isMobile && !string.IsNullOrEmpty(category.ArticleMobileTmplName.Trim()))
                        {
                            tmplName = category.ArticleMobileTmplName;
                        }
                        else if (!string.IsNullOrEmpty(category.ArticleTmplName.Trim()))
                        {
                            tmplName = category.ArticleTmplName;
                        }

                    }
                    return new PageTemplate
                    {
                        templatePath = templateDir,
                        templateUrl = templateDir + "/" + tmplName,
                        stylePath = stylePath
                    };
            }
            return new PageTemplate();
        }
        private bool getIsMobile()
        {
            bool isMobile = bool.Parse(sysConfigs["cfg_mob_enable"]);
            if (!isMobile)
            {
                return false;
            }
            if (string.IsNullOrEmpty(sysConfigs["cfg_mob_flag"].Trim()))
            {
                return false;
            }
            Regex reg = new Regex("(?:" + sysConfigs["cfg_mob_flag"] + ")", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return reg.IsMatch(Request.UserAgent);
        }

        private string ContentKeys(string content)
        {
            var keys = CacheHelper.GetKeywords();
            string tmpl = sysConfigs["cfg_link_template"];
            bool keyEnable = Convert.ToBoolean(sysConfigs["cfg_artkey_enable"]);
            if (!keyEnable)
            {
                return content;
            }
            int keyNumber = Convert.ToInt32(sysConfigs["cfg_artkey_rn"]);
            int start = 1;
            foreach (var k in keys)
            {
                if (content.Contains(k.Keyword))
                {
                    Regex reg = new Regex(k.Keyword, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    string url = tmpl.Replace("{url}", k.Url).Replace("{name}", k.Keyword);
                    content = reg.Replace(content, url, 1);
                }
                if (keyNumber < start++)
                {
                    break;
                }
            }
            return content;
        }
    }
}