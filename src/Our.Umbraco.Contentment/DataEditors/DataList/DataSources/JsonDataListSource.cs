﻿/* Copyright © 2019 Lee Kelleher, Umbrella Inc and other contributors.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Contentment.DataEditors
{
#if !DEBUG
    // TODO: IsWorkInProgress - Under development.
    [global::Umbraco.Core.Composing.HideFromTypeFinder]
#endif
    internal class JsonDataListSource : IDataListSource
    {
        public string Name => "JSON";

        public string Description => "Configure the data source to use JSON data.";

        public string Icon => "icon-brackets";

        // TODO: [LK:2019-06-13] Might need a "Notes" field at the top, to explain how these JSONPath queries work, (as I have no idea myself!)

        [ConfigurationField("url", "URL", "textstring", Description = "Enter the URL of the JSON data source.")]
        public string Url { get; set; }

        // TODO: [LK:2019-06-13] I have no idea how JSONPath works! Everytime I read the docs, it doesn't make any sense. How would you get the string-value from a "key"? and how do you get the text-value?
        // Tempted to say querying unstructured JSON is stupid.

        //[ConfigurationField("itemsJsonPath", "Items JSONPath", "textstring", Description = "Enter the JSONPath expression to select the items from the JSON data source.")]
        //public string ItemsJsonPath { get; set; }

        //[ConfigurationField("nameJsonPath", "Name JSONPath", "textstring", Description = "Enter the JSONPath expression to select the name from the item.")]
        //public string NameJsonPath { get; set; }

        //[ConfigurationField("valueJsonPath", "Value JSONPath", "textstring", Description = "Enter the JSONPath expression to select the value (key) from the item.")]
        //public string ValueJsonPath { get; set; }

        public IEnumerable<DataListItem> GetItems()
        {
            // Try something like... http://country.io/names.json
            var items = new List<DataListItem>();

            var json = GetJson();

            if (json == null)
                return items;

            foreach (JProperty token in json.Children())
            {
                items.Add(new DataListItem
                {
                    Name = token.Value?.ToString(),
                    Value = token.Name
                });
            }

            return items;
        }

        private JObject GetJson()
        {
            if (string.IsNullOrWhiteSpace(Url))
                return null;

            var json = default(JObject);

            if (Url.InvariantStartsWith("http"))
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var response = client.DownloadString(Url);
                        if (string.IsNullOrWhiteSpace(response) == false)
                        {
                            json = JObject.Parse(response);
                        }
                    }
                }
                catch (WebException ex)
                {
                    // TODO: [LK:2019-06-13] Once we figure out how to DI with deserialized JSON, replace `Current.Logger`.
                    Current.Logger.Error<JsonDataListSource>(ex, "Unable to fetch remote data.");
                }
            }
            else
            {
                // assume local file
                var path = IOHelper.MapPath(Url);
                if (File.Exists(path))
                {
                    var contents = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(contents) == false)
                    {
                        json = JObject.Parse(contents);
                    }
                }
                else
                {
                    Current.Logger.Warn<JsonDataListSource>("Unable to find the local file path.");
                }
            }

            return json;
        }
    }
}
