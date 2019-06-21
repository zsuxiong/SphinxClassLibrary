# SphinxClassLibrary
SphinxClassLibrary .net 版本->.net standard


# 使用方法跟.net 版本一致
``` c#
 public static void Test()
        {
int limit = 10;
            SphinxClient sphinxClient = new SphinxClient(ip地址, 端口);
            sphinxClient.MaxMatches = 20000;
            sphinxClient.Offset = 0;
            sphinxClient.Limit = limit;
            sphinxClient.SelectList = "NewsID,NewsTitle,NewsContent,CoverUrl,Description";
            string keyword = "";
            string filter = "";
            keyword =   "\"比特币\"";
            filter = keyword + " @ChannelID \"" +1 + "\"";
            sphinxClient.Mode = MatchModes.SPH_MATCH_EXTENDED2;
            sphinxClient.Sort = SortModes.SPH_SORT_RELEVANCE| SortModes.SPH_SORT_EXTENDED;
            sphinxClient.SortBy = "IssueTime desc "; 
            string index = "index_datacenter_news";
            sphinxClient.AddQuery(filter, index, "");
            sphinxClient.FieldsWeights.Add("NewsTitle", 10); 
            List<Result> list = sphinxClient.RunQueries();
            List<string> list_title = new List<string>();
            List<string> list_content = new List<string>();
          
            foreach (Result current in list)
            {

                if (current.Status == SeachdStatusCodes.SEARCHD_OK)
                {
                    foreach (Match current2 in current.Matches)
                    {
                        list_title.Add(current2.Attributes["newstitle"].AsString);
                        list_content.Add(current2.Attributes["newscontent"].AsString);
                        
                        
                    }
                    int totalFound = current.TotalFound;
                }
            }
            if (list_title.Count > 0)
            {
                ExcerptOptions excerptOptions = new ExcerptOptions();
                excerptOptions.Flags = ExcerptFlags.SINGLE_PASSAGE;
                excerptOptions.Limit = 60;
                excerptOptions.Around = 25;
                excerptOptions.BeforeMatch = "<span class=\"red\">";
                excerptOptions.AfterMatch = "</span>";
                // 高亮
                List<string> list4 = sphinxClient.BuildExcerpts(list_title, index, keyword, excerptOptions);
                List<string> list5 = sphinxClient.BuildExcerpts(list_content, index, keyword, excerptOptions);
                //for (int j = 0; j < list4.Count; j++)
                //{
                //    newsSearchResult.NewsList[j].Title = list4[j];
                //    newsSearchResult.NewsList[j].Description = list5[j];
                //}
            }
}
```
