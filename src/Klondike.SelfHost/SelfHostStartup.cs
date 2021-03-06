﻿using System;
using System.Net;
using Autofac;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using NuGet.Lucene.Web;
using NuGet.Lucene.Web.Formatters;
using Owin;

namespace Klondike.SelfHost
{
    class SelfHostStartup : Klondike.Startup
    {
        private readonly SelfHostSettings selfHostSettings;

        public SelfHostStartup(SelfHostSettings selfHostSettings)
        {
            this.selfHostSettings = selfHostSettings;
        }

        protected override IContainer CreateContainer(IAppBuilder app)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(selfHostSettings.VirtualPathUtility).As<IVirtualPathUtility>();
            var container = base.CreateContainer(app);
            builder.Update(container);
            return container;
        }

        protected override INuGetWebApiSettings CreateSettings()
        {
            return selfHostSettings;
        }

        protected override void Start(IAppBuilder app, IContainer container)
        {
            if (selfHostSettings.EnableIntegratedWindowsAuthentication)
            {
                ConfigureAuthentication(app);
            }

            base.Start(app, container);

            var fileServerOptions = new FileServerOptions
            {
                FileSystem = new PhysicalFileSystem(selfHostSettings.BaseDirectory),
                RequestPath = new PathString(selfHostSettings.VirtualPathRoot.TrimEnd('/')),
                EnableDefaultFiles = true
            };
            fileServerOptions.DefaultFilesOptions.DefaultFileNames = new[] {"index.html"};
            app.UseFileServer(fileServerOptions);
        }

        private static void ConfigureAuthentication(IAppBuilder app)
        {
            object listenerObj;
            if (!app.Properties.TryGetValue("System.Net.HttpListener", out listenerObj))
            {
                throw new InvalidOperationException("Integrated Windows Authentication can only be enabled when using Microsoft.Owin.Host.HttpListener Server Factory.");
            }
            
            var listener = (HttpListener) listenerObj;

            listener.AuthenticationSchemeSelectorDelegate = req =>
            {
                var path = req.Url.GetComponents(UriComponents.Path, UriFormat.UriEscaped);
                if (path.EndsWith("api/authenticate"))
                {
                    return AuthenticationSchemes.IntegratedWindowsAuthentication;
                }
                return AuthenticationSchemes.Anonymous;
            };

            listener.AuthenticationSchemes =
                AuthenticationSchemes.IntegratedWindowsAuthentication
                | AuthenticationSchemes.Anonymous;
        }
    }
}
