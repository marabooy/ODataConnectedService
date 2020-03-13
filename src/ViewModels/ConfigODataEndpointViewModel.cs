// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.OData.ConnectedService.Models;
using Microsoft.OData.ConnectedService.Views;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService.ViewModels
{
    internal class ConfigODataEndpointViewModel : ConnectedServiceWizardPage
    {
        private UserSettings userSettings;

        public string Endpoint { get; set; }
        public string ServiceName { get; set; }
        public Version EdmxVersion { get; set; }
        public string MetadataTempPath { get; set; }

        public bool IncludeWebProxy { get; set; }

        public string WebProxyHost { get; set; }

        public bool IncludeWebProxyNetworkCredentials { get; set; }

        public string WebProxyNetworkCredentialsUsername { get; set; }
        public string WebProxyNetworkCredentialsPassword { get; set; }

        public string WebProxyNetworkCredentialsDomain { get; set; }

        public event EventHandler<EventArgs> PageEntering;

        public UserSettings UserSettings
        {
            get { return this.userSettings; }
        }

        public ConfigODataEndpointViewModel(UserSettings userSettings) : base()
        {
            this.Title = "Configure endpoint";
            this.Description = "Enter or choose an OData service endpoint to begin";
            this.Legend = "Endpoint";
            this.userSettings = userSettings;

        }

        public override async Task OnPageEnteringAsync(WizardEnteringArgs args)
        {
            await base.OnPageEnteringAsync(args);
            this.View = new ConfigODataEndpoint();
            this.ResetDataContext();
            this.View.DataContext = this;
            if (PageEntering != null)
            {
                this.PageEntering(this, EventArgs.Empty);
            }
        }

        private void ResetDataContext()
        {
            this.ServiceName = Constants.DefaultServiceName;
        }

        public override Task<PageNavigationResult> OnPageLeavingAsync(WizardLeavingArgs args)
        {
            UserSettings.AddToTopOfMruList(((ODataConnectedServiceWizard)this.Wizard).UserSettings.MruEndpoints, this.Endpoint);
            Version version;
            try
            {
                this.MetadataTempPath = GetMetadata(out version);
                this.EdmxVersion = version;
                return base.OnPageLeavingAsync(args);
            }
            catch (Exception e)
            {
                return Task.FromResult<PageNavigationResult>(
                    new PageNavigationResult()
                    {
                        ErrorMessage = e.Message,
                        IsSuccess = false,
                        ShowMessageBoxOnFailure = true
                    });
            }
        }

        private string GetMetadata(out Version edmxVersion)
        {
            if (String.IsNullOrEmpty(this.Endpoint))
            {
                throw new ArgumentNullException("OData Service Endpoint", "Please input the service endpoint");
            }

            if (this.Endpoint.StartsWith("https:", StringComparison.Ordinal)
                || this.Endpoint.StartsWith("http", StringComparison.Ordinal))
            {
                if (!this.Endpoint.EndsWith("$metadata", StringComparison.Ordinal))
                {
                    this.Endpoint = this.Endpoint.TrimEnd('/') + "/$metadata";
                }
            }



            // Set up XML secure resolver
            XmlUrlResolver xmlUrlResolver = new XmlUrlResolver()
            {
                Credentials = CredentialCache.DefaultNetworkCredentials

            };

            if (IncludeWebProxy)
            {
                WebProxy proxy = new WebProxy(WebProxyHost);
                if (IncludeWebProxyNetworkCredentials)
                {
                    proxy.Credentials = new NetworkCredential(WebProxyNetworkCredentialsUsername, WebProxyNetworkCredentialsPassword, WebProxyNetworkCredentialsDomain);
                }

                xmlUrlResolver.Proxy = proxy;
            }

            PermissionSet permissionSet = new PermissionSet(System.Security.Permissions.PermissionState.Unrestricted);

            XmlReaderSettings readerSettings = new XmlReaderSettings()
            {
                XmlResolver = new XmlSecureResolver(xmlUrlResolver, permissionSet)
            };

            string workFile = Path.GetTempFileName();

            try
            {
                using (XmlReader reader = XmlReader.Create(this.Endpoint, readerSettings))
                {
                    using (XmlWriter writer = XmlWriter.Create(workFile))
                    {
                        while (reader.NodeType != XmlNodeType.Element)
                        {
                            reader.Read();
                        }

                        if (reader.EOF)
                        {
                            throw new InvalidOperationException("The metadata is an empty file");
                        }

                        Common.Constants.SupportedEdmxNamespaces.TryGetValue(reader.NamespaceURI, out edmxVersion);
                        writer.WriteNode(reader, false);
                    }
                }
                return workFile;
            }
            catch (WebException e)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Cannot access {0}", this.Endpoint), e);
            }
        }
    }
}
