﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Dexter.Core.Provider;
using Dexter.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;
using Umbraco.Core.Services;
using Dexter.Core.Models.Config;

namespace Dexter.Core.Controllers.Api
{
    public class DexterBackofficeApiController : UmbracoAuthorizedApiController
    {
        protected DexterConfigProvider ConfigProvider;

        public DexterBackofficeApiController()
        {
            var fileSystemService = new FileSystemService(new DirectoryInfo(HttpContext.Current.Server.MapPath("~")));
            ConfigProvider = new DexterConfigProvider(fileSystemService);
        }

        [HttpGet]
        public void Reindex(string index)
        {
            var config = ConfigProvider.GetIndexConfig(index);
            
            var indexService = new IndexService(ConfigProvider);

            indexService.ClearIndex(index);

            if(config.ContentTypes.Count > 0)
            {
                ReindexContent(config, indexService, index);
            }

            if (config.MediaTypes.Count > 0)
            {
                ReindexMedia(config, indexService, index);
            }
        }

        private void ReindexContent(Index config, IndexService indexService, string index)
        {
            var contentTypes = Services.ContentTypeService.GetAllContentTypes().ToArray();
            foreach (var contentType in config.ContentTypes)
            {
                var contentTypeId = contentTypes?.FirstOrDefault(c => c.Alias == contentType.Alias)?.Id ?? 0;

                if (contentTypeId == 0)
                    continue;

                var contents = Services.ContentService.GetContentOfContentType(contentTypeId).Where(c => c.Published);

                foreach (var content in contents)
                {
                    indexService.Index(content, Models.Indexable.Source.Content, index);
                }
            }
        }

        private void ReindexMedia(Index config, IndexService indexService, string index)
        {
            var mediaTypes = Services.ContentTypeService.GetAllMediaTypes().ToArray();
            foreach (var mediaType in config.MediaTypes)
            {
                var mediaTypeId = mediaTypes?.FirstOrDefault(c => c.Alias == mediaType.Alias)?.Id ?? 0;

                if (mediaTypeId == 0)
                    continue;

                var medias = Services.MediaService.GetMediaOfMediaType(mediaTypeId);

                foreach (var media in medias)
                {
                    indexService.Index(media, Models.Indexable.Source.Media, index);
                }
            }
        }

        public IEnumerable<Models.Backoffice.Index> GetIndexes(string indexName = "")
        {
            var config = ConfigProvider.GetRootConfig();
            var indexService = new IndexService(ConfigProvider);
            var indexer = indexService.LoadIndexer(config);

            var indexes = new List<Models.Backoffice.Index>();

            foreach(var name in config.Indexes)
            {
                if (indexName != "" && name != indexName)
                    continue;

                var index = new Models.Backoffice.Index();
                index.Name = name;
                index.DocumentsIndexed = indexer.GetNumberOfDocumentsStored(name);
                indexes.Add(index);
            }

            return indexes;
        } 
    }
}
