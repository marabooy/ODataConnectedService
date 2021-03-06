﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService
{
    [ConnectedServiceProviderExport(Constants.ProviderId, SupportsUpdate = true)]
    internal class ODataConnectedServiceProvider : ConnectedServiceProvider
    {
        public ODataConnectedServiceProvider()
        {
            Name = "OData Connected Service";
            Category = "OData";
            Description = "OData Connected Service for V1-V4";
            Icon = new BitmapImage(new Uri("pack://application:,,/" + this.GetType().Assembly.ToString() + ";component/Resources/Icon.png"));
            CreatedBy = "OData";
            Version = new Version(0, 5, 1);
            MoreInfoUri = new Uri("https://github.com/odata/ODataConnectedService");
        }

        public override Task<ConnectedServiceConfigurator> CreateConfiguratorAsync(ConnectedServiceProviderContext context)
        {
            var wizard = new ODataConnectedServiceWizard(context);
            return Task.FromResult<ConnectedServiceConfigurator>(wizard);
        }

        public override IEnumerable<Tuple<string, Uri>> GetSupportedTechnologyLinks()
        {
            yield return new Tuple<string, Uri>("OData Website", new Uri("http://www.odata.org/"));
            yield return new Tuple<string, Uri>("OData Docs and Samples", new Uri("http://odata.github.io/odata.net/"));
        }
    }
}