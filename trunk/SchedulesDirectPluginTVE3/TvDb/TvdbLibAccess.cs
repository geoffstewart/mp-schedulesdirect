/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 3/25/2010
 * Time: 8:47 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using TvdbLib;
using TvdbLib.Cache;
using TvdbLib.Data;
//using log4net;
using System.Collections.Generic;
using System.Collections;
using TvLibrary.Log;
using SchedulesDirect.Plugin;

namespace SchedulesDirect.TvDb
{
	/// <summary>
	/// Description of TvdbLibAccess.
	/// </summary>
	public class TvdbLibAccess
	{
		//private static ILog log = LogManager.GetLogger(typeof(TvdbLibAccess));
		private TvdbHandler tvdbHandler;
		private bool logdebug = false;
		private Hashtable seriesNameMapping = new Hashtable();
		private List<string> seriesExplicit = new List<string>();
		
		public TvdbLibAccess()
		{
		  init();
		}
		
		public TvdbLibAccess(bool logDebug)
		{
		  logdebug = logDebug;
		  init();
		}
		
		private void init() {
		  string cache = Environment.ExpandEnvironmentVariables(PluginSettings.TvDbLibCache);
		  string tvdbid = "BBB734ABE146900D";  // mine, don't abuse it!!!
		  tvdbHandler = new TvdbHandler(new XmlCacheProvider(cache),tvdbid);
		  tvdbHandler.InitCache();
		
		  seriesNameMapping = PluginSettings.TvDbSeriesMappings;
		}
		
		public void closeCache() {
			tvdbHandler.CloseCache();
		}
		
		public string getSeriesId(string seriesName) {
		  string searchSeries = seriesName;
		  if (seriesNameMapping.Contains(seriesName)) {
		    if (((string)seriesNameMapping[seriesName]).StartsWith("id=")) {
   	      // we're doing a direct mapping from series to tvdb.com id
   	      string seriesid = ((string)seriesNameMapping[seriesName]).Substring(3);
   	      if (logdebug) {
   		     Log.Debug("SD-TvDb: Direct mapping: series: " + searchSeries + "  id: " + seriesid);
   		   }
   	      return seriesid;
   	    }
		    searchSeries = (string)seriesNameMapping[seriesName];
		  }
			List<TvdbSearchResult> l = tvdbHandler.SearchSeries(searchSeries);

			if (logdebug) {
			  Log.Debug("SD-TvDb: Search for " + searchSeries + " returned this many results: " + l.Count);
			}
			if (l.Count >= 1) {
			  for (int i=0; i < l.Count; i++) {
			    //Log.WriteFile("seriesin: " + seriesName.ToLower() + "   result: " + l[0].SeriesName.ToLower());
			    if (searchSeries.ToLower().Equals(l[i].SeriesName.ToLower())) {
			      string id = "" + l[i].Id;
			      if (logdebug) {
			        Log.Debug("SD-TvDb: series: " + searchSeries + "  id: " + id);
			      }
			      return id;
			    }
			  }

				Log.Info("SD-TvDb: Could not find series match: {0} renamed {1}", seriesName, searchSeries);
			  return "";
			} else {
				return "";
			}
		}
		
	
		public string getSeasonEpisode(string seriesName, string seriesId, string episodeName, DateTime origAirDate, bool allowTailMatch) {
		  return getSeasonEpisode(seriesName,seriesId,episodeName,origAirDate, allowTailMatch,false);
		}
		
		public string getSeasonEpisode(string seriesName, string seriesId, string episodeName, DateTime origAirDate, bool allowTailMatch, bool recurseCall) {
		  if (seriesId == null || seriesId.Length == 0) {
		    return "";
		  }
			TvdbSeries s = tvdbHandler.GetSeries(Convert.ToInt32(seriesId),TvdbLanguage.DefaultLanguage,true,false,false);
			List<TvdbEpisode> el = s.Episodes;
			
			foreach (TvdbEpisode e in el) {
			  //Log.WriteFile("Compare 1: " + episodeName + "  -> " + e.EpisodeName);
				if (episodeName.Equals(e.EpisodeName) ||
			      episodeName.ToLower().Equals(e.EpisodeName.ToLower())) {
					int sn = e.SeasonNumber;
					int ep = e.EpisodeNumber;
					string ret;
					
					if (sn < 10) {
						ret = "S0" + sn;
					} else {
						ret = "S" + sn;
					}
					if (ep < 10) {
						ret += "E0" + ep;
					} else {
						ret += "E" + ep;
					}
					
					if (logdebug) {
					  Log.Debug("SD-TvDb: TvDb match (compare1): {0} -> {1}",episodeName,e.EpisodeName);
					}
					return ret;
				}
			  
			  // remove all punctuation
			  string e1 = removePunctuation(episodeName);
			  string e2 = removePunctuation(e.EpisodeName);
			  //Log.WriteFile("Compare 2: " + e1 + "  -> " + e2);
			  
			  if (e1.Equals(e2) ||
			      e1.ToLower().Equals(e2.ToLower())) {
					int sn = e.SeasonNumber;
					int ep = e.EpisodeNumber;
					string ret;
					
					if (sn < 10) {
						ret = "S0" + sn;
					} else {
						ret = "S" + sn;
					}
					if (ep < 10) {
						ret += "E0" + ep;
					} else {
						ret += "E" + ep;
					}
					if (logdebug) {
					  Log.Debug("SD-TvDb: TvDb match (compare2): {0} -> {1}",e1,e2);
					}
					return ret;
				}
			}
			
			int SHORTMATCH = 10;
			
			if (episodeName.Length >= SHORTMATCH) {
				// still here.. try matching a smaller part of the episode name
			
				string shortEpisodeName = episodeName.Substring(0,SHORTMATCH);
				
			  foreach (TvdbEpisode e in el) {
					if (e.EpisodeName.Length >= SHORTMATCH) {
						string shortDbEpName = e.EpisodeName.Substring(0,SHORTMATCH);
						//Log.WriteFile("compare 3: " + shortEpisodeName.ToLower() + "    -> "  + shortDbEpName.ToLower());
						
						if (shortEpisodeName.ToLower().Equals(shortDbEpName.ToLower())) {
							int sn = e.SeasonNumber;
							int ep = e.EpisodeNumber;
							string ret;
							
							if (sn < 10) {
								ret = "S0" + sn;
							} else {
								ret = "S" + sn;
							}
							if (ep < 10) {
								ret += "E0" + ep;
							} else {
								ret += "E" + ep;
							}
							if (logdebug) {
					    	Log.Debug("SD-TvDb: TvDb match (compare3): {0} -> {1}",shortEpisodeName.ToLower(),shortDbEpName.ToLower());
							}
							return ret;
						}
						
    			  // remove all punctuation
    			  string e1 = removePunctuation(episodeName);
    			  string e2 = removePunctuation(e.EpisodeName);
    			  //Log.WriteFile("Compare 4: " + e1 + "  -> " + e2);
    			  if (e1.Equals(e2) ||
    			      e1.ToLower().Equals(e2.ToLower())) {
    					int sn = e.SeasonNumber;
    					int ep = e.EpisodeNumber;
    					string ret;
    					
    					if (sn < 10) {
    						ret = "S0" + sn;
    					} else {
    						ret = "S" + sn;
    					}
    					if (ep < 10) {
    						ret += "E0" + ep;
    					} else {
    						ret += "E" + ep;
    					}
    					if (logdebug) {
					    	Log.Debug("SD-TvDb: TvDb match (compare4): {0} -> {1}",e1,e2);
    					}
    					return ret;
    				}
    			  
    			  if (allowTailMatch) {
      			  string endEpisodeName = episodeName.Substring(episodeName.Length - SHORTMATCH);
  				
      			  string endDbEpName = e.EpisodeName.Substring(e.EpisodeName.Length - SHORTMATCH);
  						//Log.WriteFile("compare 5: " + endEpisodeName.ToLower() + "    -> "  + endDbEpName.ToLower());

  						if (endEpisodeName.ToLower().Equals(endDbEpName.ToLower())) {
  							int sn = e.SeasonNumber;
  							int ep = e.EpisodeNumber;
  							string ret;
  							
  							if (sn < 10) {
  								ret = "S0" + sn;
  							} else {
  								ret = "S" + sn;
  							}
  							if (ep < 10) {
  								ret += "E0" + ep;
  							} else {
  								ret += "E" + ep;
  							}
  							if (logdebug) {
					        Log.Debug("SD-TvDb: TvDb match (compare5): {0} -> {1}",endEpisodeName.ToLower(),endDbEpName.ToLower());
  							}
  							return ret;
  						}
  						
      			  // remove all punctuation
      			  e1 = removePunctuation(endEpisodeName);
      			  e2 = removePunctuation(endDbEpName);
      			  //Log.WriteFile("Compare 6: " + e1 + "  -> " + e2);
      			  if (e1.Equals(e2) ||
      			      e1.ToLower().Equals(e2.ToLower())) {
      					int sn = e.SeasonNumber;
      					int ep = e.EpisodeNumber;
      					string ret;
      					
      					if (sn < 10) {
      						ret = "S0" + sn;
      					} else {
      						ret = "S" + sn;
      					}
      					if (ep < 10) {
      						ret += "E0" + ep;
      					} else {
      						ret += "E" + ep;
      					}
      					if (logdebug) {
					        Log.Debug("SD-TvDb: TvDb match (compare6): {0} -> {1}",e1,e2);
      					}
      					return ret;
      				}
    			  }
 					}
				}
				
				
			}
			
			if (origAirDate != null && origAirDate.Date != null) {
			  
			
			  foreach (TvdbEpisode e in el) {
			    if (e.FirstAired.Equals(origAirDate)) {
			      int sn = e.SeasonNumber;
  					int ep = e.EpisodeNumber;
  					string ret;
  					
  					if (sn < 10) {
  						ret = "S0" + sn;
  					} else {
  						ret = "S" + sn;
  					}
  					if (ep < 10) {
  						ret += "E0" + ep;
  					} else {
  						ret += "E" + ep;
  					}
  					if (logdebug) {
			        Log.Debug("SD-TvDb: TvDb match (compare7): {0} date {1}",episodeName,origAirDate);
  					}
  					return ret;  
			    }
			  }
			}
			
			if (!recurseCall) {
			  TvdbSeries tvdbSeries = this.tvdbHandler.GetFullSeries(Convert.ToInt32(seriesId),TvdbLanguage.DefaultLanguage,false);
			
			  this.tvdbHandler.ForceReload(tvdbSeries,true,true,false);
			  return getSeasonEpisode(seriesName,seriesId,episodeName,origAirDate,allowTailMatch,true);
			}
			
			Log.Info("SD-TvDb: No episode match for series: {0}, seriesId: {1}, episodeName: {2}",seriesName,seriesId,episodeName);
			// still can't match.. return nothing
			return "";
		}
		
		public static string stripPunctuation(string s) {

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < s.Length; i++) {
        
          if (Char.IsLetterOrDigit(s[i])) {
            sb = sb.Append(s[i]);
        }
      }
      
      return sb.ToString();
    }
		
		private string removePunctuation(string inString) {
		  return stripPunctuation(inString);
		}
		
		public bool shouldWeGetTvDbForSeries(string seriesName) {
      string sn = stripPunctuation(seriesName).ToLower();
      
      // check schedules... if we scheduled it, we want to get the tvdb info      
      IList<TvDatabase.Schedule> showNames = TvDatabase.Schedule.ListAll();
      foreach(TvDatabase.Schedule s in showNames) {
        string longS = s.ProgramName;
        string shortS = stripPunctuation(longS).ToLower();
        if (shortS.Equals(sn)) {
          return true;
        }
      }
      
      // still here? we don't get tvdb.com info for this series
      return false;
    }
	}
}
