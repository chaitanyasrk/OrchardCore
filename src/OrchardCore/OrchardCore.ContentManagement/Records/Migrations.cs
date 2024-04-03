using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Utilities;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.ContentManagement.Records
{
    public class Migrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentManager _contentManager;
        private readonly IServiceProvider _serviceProvider;

        public Migrations(IContentDefinitionManager contentDefinitionManager,
            IContentManager contentManager,
            IServiceProvider serviceProvider)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentManager = contentManager;
            _serviceProvider = serviceProvider;
        }

        public async Task<int> CreateAsync()
        {
            await SchemaBuilder.CreateMapIndexTableAsync<ContentItemIndex>(table => table
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<string>("ContentItemVersionId", c => c.WithLength(26))
                .Column<bool>("Latest")
                .Column<bool>("Published")
                .Column<string>("ContentType", column => column.WithLength(ContentItemIndex.MaxContentTypeSize))
                .Column<DateTime>("ModifiedUtc", column => column.Nullable())
                .Column<DateTime>("PublishedUtc", column => column.Nullable())
                .Column<DateTime>("CreatedUtc", column => column.Nullable())
                .Column<string>("Owner", column => column.Nullable().WithLength(ContentItemIndex.MaxOwnerSize))
                .Column<string>("Author", column => column.Nullable().WithLength(ContentItemIndex.MaxAuthorSize))
                .Column<string>("DisplayText", column => column.Nullable().WithLength(ContentItemIndex.MaxDisplayTextSize))
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId",
                    "DocumentId",
                    "ContentItemId",
                    "ContentItemVersionId",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_ContentType",
                    "DocumentId",
                    "ContentType",
                    "CreatedUtc",
                    "ModifiedUtc",
                    "PublishedUtc",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_Owner",
                    "DocumentId",
                    "Owner",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_Author",
                    "DocumentId",
                    "Author",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_DisplayText",
                    "DocumentId",
                    "DisplayText",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_Published",
                    "DocumentId",
                    "ContentItemId",
                    "Published",
                    "Latest")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 6;
        }

        public async Task<int> UpdateFrom1Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .AddColumn<string>("ContentItemVersionId", c => c.WithLength(26))
            );

            return 2;
        }

        public async Task<int> UpdateFrom2Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .AddColumn<string>("DisplayText", column => column.Nullable().WithLength(ContentItemIndex.MaxDisplayTextSize))
            );

            return 3;
        }

        // Migrate content type definitions. This only needs to run on old content definition schemas.
        // This code can be removed in a later version.
        public async Task<int> UpdateFrom3Async()
        {
            var contentTypeDefinitions = await _contentDefinitionManager.LoadTypeDefinitionsAsync();
            foreach (var contentTypeDefinition in contentTypeDefinitions)
            {
                var existingContentTypeSettings = contentTypeDefinition.Settings.ToObject<ContentTypeSettings>();

                // Do this before creating builder, so settings are removed from the builder settings object.
                // Remove existing properties from JObject
                var contentTypeSettingsProperties = existingContentTypeSettings.GetType().GetProperties();
                foreach (var property in contentTypeSettingsProperties)
                {
                    contentTypeDefinition.Settings.Remove(property.Name);
                }

                await _contentDefinitionManager.AlterTypeDefinitionAsync(contentTypeDefinition.Name, builder =>
                {
                    builder.WithSettings(existingContentTypeSettings);

                    foreach (var contentTypePartDefinition in contentTypeDefinition.Parts)
                    {
                        var existingTypePartSettings = contentTypePartDefinition.Settings.ToObject<ContentTypePartSettings>();

                        // Remove existing properties from JObject
                        var contentTypePartSettingsProperties = existingTypePartSettings.GetType().GetProperties();
                        foreach (var property in contentTypePartSettingsProperties)
                        {
                            contentTypePartDefinition.Settings.Remove(property.Name);
                        }

                        builder.WithPart(contentTypePartDefinition.Name, contentTypePartDefinition.PartDefinition, partBuilder =>
                        {
                            partBuilder.WithSettings(existingTypePartSettings);
                        });
                    }
                });
            }

            return 4;
        }

        // Migration content part definitions.
        // This code can be removed in a later version.
        public async Task<int> UpdateFrom4Async()
        {
            var partDefinitions = await _contentDefinitionManager.LoadPartDefinitionsAsync();
            foreach (var partDefinition in partDefinitions)
            {
                var existingPartSettings = partDefinition.Settings.ToObject<ContentPartSettings>();

                // Do this before creating builder, so settings are removed from the builder settings object.
                // Remove existing properties from JObject
                var contentTypeSettingsProperties = existingPartSettings.GetType().GetProperties();
                foreach (var property in contentTypeSettingsProperties)
                {
                    partDefinition.Settings.Remove(property.Name);
                }

                await _contentDefinitionManager.AlterPartDefinitionAsync(partDefinition.Name, partBuilder =>
                {
                    partBuilder.WithSettings(existingPartSettings);
                    foreach (var fieldDefinition in partDefinition.Fields)
                    {
                        var existingFieldSettings = fieldDefinition.Settings.ToObject<ContentPartFieldSettings>();

                        // Do this before creating builder, so settings are removed from the builder settings object.
                        // Remove existing properties from JObject
                        var fieldSettingsProperties = existingFieldSettings.GetType().GetProperties();
                        foreach (var property in fieldSettingsProperties)
                        {
                            fieldDefinition.Settings.Remove(property.Name);
                        }

                        partBuilder.WithField(fieldDefinition.Name, fieldBuilder =>
                        {
                            fieldBuilder.WithSettings(existingFieldSettings);
                        });
                    }
                });
            }

            return 5;
        }

        // This code can be removed in a later version.
        public async Task<int> UpdateFrom5Async()
        {
            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId",
                    "DocumentId",
                    "ContentItemId",
                    "ContentItemVersionId",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_ContentType",
                    "DocumentId",
                    "ContentType",
                    "CreatedUtc",
                    "ModifiedUtc",
                    "PublishedUtc",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_Owner",
                    "DocumentId",
                    "Owner",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_Author",
                    "DocumentId",
                    "Author",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
                .CreateIndex("IDX_ContentItemIndex_DocumentId_DisplayText",
                    "DocumentId",
                    "DisplayText",
                    "Published",
                    "Latest")
            );

            await SchemaBuilder.AlterIndexTableAsync<ContentItemIndex>(table => table
               .CreateIndex("IDX_ContentItemIndex_DocumentId_Published",
                   "DocumentId",
                   "ContentItemId",
                   "Published",
                   "Latest")
           );

            return 6;
        }

        
        //public async Task<int> UpdateFrom6Async()
        //{
        //    var contents = new List<ContentItem>();
        //    for (int i = 0; i < 10; i++)
        //    {
        //        var sampleContent = @"{
        //                                    ""AutoroutePart"": {
        //                                        ""Path"": ""blog/post-1""
        //                                    },
        //                                    ""BlogPost"": {
        //                                        ""Category"": {
        //                                            ""TaxonomyContentItemId"": ""4dqxtgevwep72v89arzk60t45q{{i}}"",
        //                                            ""TermContentItemIds"": [
        //                                                ""49bxdfhtz9cs02qvt6q6acy5w6{{i}}""
        //                                            ]
        //                                        },
        //                                        ""Image"": {
        //                                            ""Anchors"": [
        //                                                {
        //                                                    ""X"": 0.5,
        //                                                    ""Y"": 0.5
        //                                                }
        //                                            ],
        //                                            ""MediaTexts"": [
        //                                                """"
        //                                            ],
        //                                            ""Paths"": [
        //                                                ""post-bg.jpg""
        //                                            ]
        //                                        },
        //                                        ""Subtitle"": {
        //                                            ""Text"": ""Problems look mighty small from 150 miles up""
        //                                        },
        //                                        ""Tags"": {
        //                                            ""TagNames"": [
        //                                                ""Earth"",
        //                                                ""Exploration"",
        //                                                ""Space""
        //                                            ],
        //                                            ""TaxonomyContentItemId"": ""4wtm8k72efp6p7ngps3360qs5z{{i}}"",
        //                                            ""TermContentItemIds"": [
        //                                                ""4vsweqehcg1ye6av57e2ch31wp{{i}}"",
        //                                                ""4ehetdn22fph67nmptm1tnvxct{{i}}"",
        //                                                ""4qrxawhkrt8nhzcre9hwt00rws{{i}}""
        //                                            ]
        //                                        }
        //                                    },
        //                                    ""ContainedPart"": {
        //                                        ""ListContentItemId"": ""4wvya27yrjjk3xxj3jwg2p0545{{i}}"",
        //                                        ""Order"": 0
        //                                    },
        //                                    ""MarkdownBodyPart"": {
        //                                        ""Markdown"": ""Never in all their history have men been able truly to conceive of the world as one: a single sphere, a globe, having the qualities of a globe, a round earth in which all the directions eventually meet, in which there is no center because every point, or none, is center — an equal earth which all men occupy as equals. The airman's earth, if free men make it, will be truly round: a globe in practice, not in theory.\n\nScience cuts two ways, of course; its products can be used for both good and evil. But there's no turning back from science. The early warnings about technological dangers also come from science.\n\nWhat was most significant about the lunar voyage was not that man set foot on the Moon but that they set eye on the earth.\n\nA Chinese tale tells of some men sent to harm a young girl who, upon seeing her beauty, become her protectors rather than her violators. That's how I felt seeing the Earth for the first time. I could not help but love and cherish her.\n\nFor those who have seen the Earth from space, and for the hundreds and perhaps thousands more who will, the experience most certainly changes your perspective. The things that we share in our world are far more valuable than those which divide us.""
        //                                    },
        //                                    ""TitlePart"": {
        //                                        ""Title"": ""Man must explore, and this is exploration at its greatest""
        //                                    }
        //                                }";
        //        var myContentItem = new ContentItem();
        //        myContentItem.DisplayText = $"Blog__{i}";
        //        myContentItem.Published = true;
        //        sampleContent.Replace("{{i}}", $"{i}");
        //        myContentItem.Data = JObject.Parse(sampleContent);
        //        myContentItem.ContentItemId = Guid.NewGuid().ToString();
        //        contents.Add(myContentItem);
        //    }

        //    using (var scope = _serviceProvider.CreateScope())
        //    {
        //        var contentManager = scope.ServiceProvider.GetRequiredService<IContentManager>();
        //        await _contentManager.ImportAsync(contents);
        //    }

        //    return 7;
        //}
    }
}
