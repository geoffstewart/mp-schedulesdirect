using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

using TvEngine;
using TvControl;
using TvDatabase;
using TvLibrary.Log;
using TvLibrary.Channels;
using Gentle.Common;
using Gentle.Framework;
using SchedulesDirect.Plugin;
using SchedulesDirect.TvDb;

namespace SchedulesDirect.Import
{
   public class EpgListingsImporter : IDisposable
   {
      private const string XMLTVID = ".schedulesdirect.org";
      private const string ZAP2ITXMLTVID = ".labs.zap2it.com";

      const int NOTFOUND = -1;

      /// <summary>
      /// Delegate for hooking into status update during import operations.
      /// </summary>
      /// <param name="stats"></param>
      public delegate void ShowProgressHandler(object sender, ImportStats stats);
      public event ShowProgressHandler ShowProgress;

      private int    _delay;
      private bool   _createChannels                = true;
      private bool   _renameChannels                = false;
      private bool   _allowChannelNumberMapping     = false;
      private bool   _sortChannels                  = false;
      private string _channelNameTemplate           = "{channel} {callsign}";
      private bool   _lineupHasChanged              = false;
      private bool   _remapChannelsOnLineupChange   = false;
      private bool   _addExtraInfoToShowDescription = false;
      private bool   _updatingNext24Hours           = false;
      private bool   _createAnalogChannels          = false;
      private bool   _useTvDb                       = true;

      private TvLibrary.Country _ExternalInputCountry = null;
      private TvLibrary.Implementations.AnalogChannel.VideoInputType _ExternalInput = TvLibrary.Implementations.AnalogChannel.VideoInputType.SvhsInput1;

      private Dictionary<string, object> _mpEpgMappingCache = new Dictionary<string, object>();
      private Dictionary<int, ATSCChannel> _mpAtscChannelCache = new Dictionary<int, ATSCChannel>();
      private Dictionary<string, int> _mpRatingAgeCache = new Dictionary<string,int>();

      private SchedulesDirect.SoapEntities.DownloadResults _results;

      private TvBusinessLayer tvLayer = new TvBusinessLayer();

      private System.Collections.IList _mpChannelCache;
      private List<Channel> _mpUnMappedChannelCache = new List<Channel>();

      #region Constructor
      /// <summary>
      /// Initializes a new instance of the <see cref="T:EpgListingsImporter"/> class.
      /// </summary>
      /// <param name="results">The results.</param>
      public EpgListingsImporter(SchedulesDirect.SoapEntities.DownloadResults results)
      {
         this._results = results;
      }
      #endregion

      #region Fields
      /// <summary>
      /// Gets or sets a value indicating whether the import should create channels it can't find.
      /// </summary>
      /// <value><c>true</c> if [create channels]; otherwise, <c>false</c>.</value>
      public bool CreateChannels
      {
         get { return _createChannels; }
         set { _createChannels = value; }
      }

      /// <summary>
      /// Gets or sets a value indicating whether the import should create analog channels it can't find.
      /// </summary>
      /// <value><c>true</c> if [create channels]; otherwise, <c>false</c>.</value>
      public bool CreateAnalogChannels
      {
         get { return _createAnalogChannels; }
         set { _createAnalogChannels = value; }
      }

      /// <summary>
      /// Gets or sets a value indicating whether to rename channels.
      /// </summary>
      /// <value><c>true</c> if [rename channels]; otherwise, <c>false</c>.</value>
      public bool RenameChannels
      {
         get { return _renameChannels; }
         set { _renameChannels = value; }
      }

      /// <summary>
      /// Gets or sets the channel name template using the following variables:
      ///     {callsign}
      ///     {name}
      ///     {affiliate}
      ///     {number}
      /// </summary>
      /// <value>The channel name template.</value>
      public string ChannelNameTemplate
      {
         get { return _channelNameTemplate; }
         set { _channelNameTemplate = value; }
      }

      /// <summary>
      /// Gets or sets the delay (in ms) during import between processing each item.
      /// </summary>
      /// <value>The delay.</value>
      public int Delay
      {
         get { return _delay; }
         set { _delay = value; }
      }

      /// <summary>
      /// Gets or sets a value indicating whether mapping by channel number only is allowed.
      /// </summary>
      /// <value><c>true</c> if [allow mapping by channel number only]; otherwise, <c>false</c>.</value>
      public bool AllowChannelNumberOnlyMapping
      {
         get { return _allowChannelNumberMapping; }
         set { _allowChannelNumberMapping = value; }
      }

      /// <summary>
      /// Gets or sets a value of the External Input Country.
      /// </summary>
      /// <value>Country for created Channels</value>
      public TvLibrary.Country ExternalInputCountry
      {
         get { return _ExternalInputCountry; }
         set { _ExternalInputCountry = value; }
      }

      /// <summary>
      /// Gets or sets a value of the External Input.
      /// </summary>
      /// <value>External Input for created Channels</value>
      public TvLibrary.Implementations.AnalogChannel.VideoInputType ExternalInput
      {
         get { return _ExternalInput; }
         set { _ExternalInput = value; }
      }

      /// <summary>
      /// Gets or sets a value indicating whether to sort the channels after import.
      /// </summary>
      /// <value><c>true</c> if [allow channel sorting]; otherwise, <c>false</c>.</value>
      public bool AllowChannelSorting
      {
         get { return _sortChannels; }
         set { _sortChannels = value; }
      }

      /// <summary>
      /// Gets or sets a value to indicate if the lineup has changed to determine if re-mapping should occur.
      /// </summary>
      public bool LineupHasChanged
      {
         get { return _lineupHasChanged; }
         set { _lineupHasChanged = value; }
      }

      /// <summary>
      /// Gets or sets a value to indicate if we should remap channels when a lineup change is detected.
      /// </summary>
      public bool RemapChannelsOnLineupChange
      {
         get { return _remapChannelsOnLineupChange; }
         set { _remapChannelsOnLineupChange = value; }
      }

      /// <summary>
      /// Gets or sets a value to indicate if we should remap channels when a lineup change is detected.
      /// </summary>
      public bool AddExtraInformationToShowDescription
      {
         get { return _addExtraInfoToShowDescription; }
         set { _addExtraInfoToShowDescription = value; }
      }

      /// <summary>
      /// Gets or sets a value to indicate if this is an update for last minute changes in the next 24 hours.
      /// </summary>
      public bool UpdatingNext24Hours
      {
         get { return _updatingNext24Hours; }
         set { _updatingNext24Hours = value; }
      }


      #endregion

      #region Verify Channel Mapping
      /// <summary>
      /// Verify Existing EPGMapping by Channel Number.
      /// If it cannot be verified then the mapping is removed to allow remapping
      /// </summary>
      public void VerifyChannelMapping()
      {
         VerifyChannelMapping(_delay);
      }

      /// <summary>
      /// Verify XmlTvId for each TVChannel exists in the current data.
      /// If it cannot be verified then the mapping is removed to allow remapping
      /// </summary>
      /// <param name="delay">The delay in ms between each record.</param>
      public void VerifyChannelMapping(int delay)
      {
        IList chList = (IList)Channel.ListAll();

         if (chList == null)
            return;

         if (chList.Count <= 0)
            return;
         
         _mpUnMappedChannelCache.Clear();

         List<string> zIdList = new List<string>();

         foreach (SchedulesDirect.SoapEntities.TVLineup lineup in _results.Data.Lineups.List)
         {
            foreach (SchedulesDirect.SoapEntities.TVStationMap tvStationMap in lineup.StationMap)
            {
               SchedulesDirect.SoapEntities.TVStation tvStation = _results.Data.Stations.StationById(tvStationMap.StationId);

               if (tvStation != null)
               {
                  string xmlId = BuildXmlTvId(lineup.IsLocalBroadcast(), tvStation, tvStationMap);
                  zIdList.Add(xmlId);
               }
            }
         }

         foreach (Channel tvCh in chList)
         {
            bool bFound = false;

            if (String.IsNullOrEmpty(tvCh.ExternalId))
               continue;

            if (tvCh.ExternalId.EndsWith(ZAP2ITXMLTVID))
            {
               tvCh.ExternalId = BuildXmlTvIdFromChannelData(tvCh, tvCh.ExternalId);
               if (tvCh.ExternalId.EndsWith(XMLTVID))
                  tvCh.Persist();
            }

            if (!tvCh.ExternalId.EndsWith(XMLTVID))
               continue;

            foreach (string xid in zIdList)
            {
               if (tvCh.ExternalId.ToLower() == xid.ToLower())
                  bFound = true;
            }

            if (!bFound)
            {
              IList chDetailList = (IList)tvCh.ReferringTuningDetail();
               if (chDetailList.Count <= 0)
                  continue;
               TuningDetail chDetail = (TuningDetail)chDetailList[0];

               string ch = chDetail.ChannelNumber.ToString();

               Log.WriteFile("EPGMapping for channel {0}-{1} (XmlTvId: {2}) no longer appears to be valid. Removing mapping to allow re-mapping.",
                  ch, tvCh.Name, tvCh.ExternalId);

               // Add to the cached if Analog and External
               if (chDetail.ChannelType == 0) // Analog
                  if (chDetail.VideoSource != (int)TvLibrary.Implementations.AnalogChannel.VideoInputType.Tuner) //External
                     _mpUnMappedChannelCache.Add(tvCh);

               string exId = tvCh.ExternalId;
               tvCh.ExternalId = String.Empty;
               tvCh.Persist();
               tvCh.ExternalId = exId; // Used for remapping

               System.Threading.Thread.Sleep(delay);
            }
         }
      }
      #endregion


      /// <summary>
      /// Imports the channels.
      /// </summary>
      /// <returns></returns>
      public int ImportChannels()
      {
         return ImportChannels(_delay);
      }

      /// <summary>
      /// Imports the channels.
      /// </summary>
      /// <param name="delay">The delay in ms between each record.</param>
      /// <returns></returns>
      public int ImportChannels(int delay)
      {
         ClearCache();

         ImportStats stats = new ImportStats();

         if (_lineupHasChanged && _remapChannelsOnLineupChange)
            VerifyChannelMapping(delay);

         // Refresh the channel cache
         _mpChannelCache = (IList)Channel.ListAll();

         foreach (SchedulesDirect.SoapEntities.TVLineup lineup in _results.Data.Lineups.List)
         {
            Log.WriteFile("Processing lineup {0} [id={1} type={2} postcode={3}]", lineup.Name, lineup.ID, lineup.Type, lineup.PostalCode);
            foreach (SchedulesDirect.SoapEntities.TVStationMap tvStationMap in lineup.StationMap)
            {
               if (ShowProgress != null)
                  ShowProgress(this, stats);

               SchedulesDirect.SoapEntities.TVStation tvStation = _results.Data.Stations.StationById(tvStationMap.StationId);
               if (tvStation == null)
               {
                  Log.WriteFile("Unable to find stationId #{0} specified in lineup", tvStationMap.StationId);
                  continue;
               }

               if (tvStationMap.ChannelMajor < 0)
               {
                  Log.WriteFile("TVStationMap ChannelMajor Not Valid. StationID: {3} ChannelMajor: {0} ChannelMinor: {1} SchedulesDirectChannel: {2}", tvStationMap.ChannelMajor, tvStationMap.ChannelMinor, tvStationMap.SchedulesDirectChannel, tvStationMap.StationId);
                  continue;
               }

               Channel mpChannel = FindTVChannel(tvStation, tvStationMap, lineup.IsLocalBroadcast());
               // Update the channel and map it
               if (mpChannel != null)
               {
                  mpChannel.GrabEpg = false;
                  mpChannel.LastGrabTime = DateTime.Now;
                  mpChannel.ExternalId = BuildXmlTvId(lineup.IsLocalBroadcast(), tvStation, tvStationMap);  //tvStation.ID + XMLTVID;
                  if (_renameChannels)
                  {
                     string oldName = mpChannel.Name;
                     mpChannel.Name = BuildChannelName(tvStation, tvStationMap);
                     mpChannel.DisplayName = BuildChannelName(tvStation, tvStationMap);
                     RenameLogo(oldName, mpChannel.Name);
                  }
                  stats._iChannels++;

                  mpChannel.Persist();

                  Log.WriteFile("Updated channel {1} [id={0} xmlid={2}]", mpChannel.IdChannel, mpChannel.Name, mpChannel.ExternalId);
               }
               else if ((_createChannels == true && lineup.IsAnalogue() == false)
                  || (_createChannels == true && _createAnalogChannels == true && lineup.IsAnalogue() == true))
               {
                  // Create the channel 
                  string cname = BuildChannelName(tvStation, tvStationMap);
                  string xId   = BuildXmlTvId(lineup.IsLocalBroadcast(), tvStation, tvStationMap);

                  mpChannel = new Channel(cname, false, true, 0, Schedule.MinSchedule, false, Schedule.MinSchedule, 10000, true, xId, true, cname);
                  mpChannel.Persist();

                  TvLibrary.Implementations.AnalogChannel tuningDetail = new TvLibrary.Implementations.AnalogChannel();

                  tuningDetail.IsRadio = false;
                  tuningDetail.IsTv = true;
                  tuningDetail.Name = cname;
                  tuningDetail.Frequency = 0;
                  tuningDetail.ChannelNumber = tvStationMap.ChannelMajor;

                  //(int)PluginSettings.ExternalInput;
                  //tuningDetail.VideoSource = PluginSettings.ExternalInput;
                  tuningDetail.VideoSource = _ExternalInput; // PluginSettings.ExternalInput;

                  // Too much overhead using settings directly for country
                  if (_ExternalInputCountry != null)
                     tuningDetail.Country = _ExternalInputCountry; // PluginSettings.ExternalInputCountry;

                  if (lineup.IsLocalBroadcast())
                     tuningDetail.TunerSource = DirectShowLib.TunerInputType.Antenna;
                  else
                     tuningDetail.TunerSource = DirectShowLib.TunerInputType.Cable;

                  //mpChannel.XMLId                  = tvStation.ID + XMLTVID;
                  //mpChannel.Name                   = BuildChannelName(tvStation, tvStationMap);
                  //mpChannel.AutoGrabEpg            = false;
                  //mpChannel.LastDateTimeEpgGrabbed = DateTime.Now;
                  //mpChannel.External               = true; // This may change with cablecard support one day
                  //mpChannel.ExternalTunerChannel   = tvStationMap.ChannelMajor.ToString();
                  //mpChannel.Frequency              = 0;
                  //mpChannel.Number                 = (int)PluginSettings.ExternalInput;

                  tvLayer.AddTuningDetails(mpChannel, tuningDetail);

                  Log.WriteFile("Added channel {1} [id={0} xmlid={2}]", mpChannel.IdChannel, mpChannel.Name, mpChannel.ExternalId);
                  stats._iChannels++;
               }
               else
               {
                  Log.WriteFile("Could not find a match for {0}/{1}", tvStation.CallSign, tvStationMap.Channel);
               }
               System.Threading.Thread.Sleep(delay);
            }
         }

         if (_lineupHasChanged && _remapChannelsOnLineupChange)
         {
            if (_mpUnMappedChannelCache != null)
            {
               foreach (Channel ch in _mpUnMappedChannelCache)
               {
                  ch.ExternalId = String.Empty;
                  ch.Persist();
               }
            }
         }

         if (_sortChannels)
            SortTVChannels();

         return stats._iChannels;
      }

      /// <summary>
      /// Imports the programs.
      /// </summary>
      /// <returns></returns>
      public int ImportPrograms()
      {
         return ImportPrograms(_delay);
      }

      /// <summary>
      /// Imports the programs.
      /// </summary>
      /// <param name="delay">The delay.</param>
      /// <returns></returns>
      public int ImportPrograms(int delay)
      {
         const string EMPTY = "-";
         StringBuilder description = new StringBuilder();
         //string strTvChannel;
         //int idTvChannel;

         ImportStats stats = new ImportStats();
         ClearCache();
         Log.WriteFile("Starting processing of {0} schedule entries", _results.Data.Schedules.List.Count);
         
         TvdbLibAccess tvdb = null;
            
         _useTvDb = PluginSettings.UseTvDb;
         bool logdebug = PluginSettings.TvDbLogDebug;
         if (_useTvDb) {
           tvdb = new TvdbLibAccess(logdebug);
         }

         foreach (SchedulesDirect.SoapEntities.TVSchedule tvSchedule in _results.Data.Schedules.List)
         {
            if (ShowProgress != null)
               ShowProgress(this, stats);

            //GetEPGMapping(tvSchedule.Station.Trim() + XMLTVID, out idTvChannel, out strTvChannel);
            List<Channel> channelIdList = GetStationChannels(tvSchedule.Station.Trim());

            if (channelIdList.Count <= 0)
            {
               Log.WriteFile("Unable to find Program Channel: StationID: #{0} XMLTVID {1} ", tvSchedule.Station, XMLTVID);
               continue;
            }

            SchedulesDirect.SoapEntities.TVProgram tvProgram = _results.Data.Programs.ProgramById(tvSchedule.ProgramId);
            if (tvProgram == null)
            {
               Log.WriteFile("Unable to find programId #{0} specified in schedule at time {1)", tvSchedule.ProgramId, tvSchedule.StartTimeStr);
               continue;
            }

            description.Length = 0; // Clears the description string builder

            DateTime localStartTime = tvSchedule.StartTimeUtc.ToLocalTime();
            DateTime localEndTime = localStartTime + tvSchedule.Duration;

            //Program mpProgram = new Program(idTvChannel, localStartTime, localEndTime, tvProgram.Title, tvProgram.
            //MediaPortal.TV.Database.TVProgram mpProgram = new TVProgram(strTvChannel, localStartTime, localEndTime, tvProgram.Title);

            string zTitle          = tvProgram.Title;
            string zEpisode        = string.IsNullOrEmpty(tvProgram.Subtitle) ? EMPTY : tvProgram.Subtitle;
            //string zDate           = string.IsNullOrEmpty(tvProgram.OriginalAirDateStr) ? EMPTY : tvProgram.OriginalAirDate.Date.ToString();
            string zDate           = string.IsNullOrEmpty(tvProgram.OriginalAirDateStr) ? DateTime.MinValue.ToString() : tvProgram.OriginalAirDate.Date.ToString();
            string zSeriesNum      = string.IsNullOrEmpty(tvProgram.Series) ? EMPTY : tvProgram.Series;
            string zEpisodeNum     = string.IsNullOrEmpty(tvProgram.SyndicatedEpisodeNumber) ? EMPTY : tvProgram.SyndicatedEpisodeNumber;
            string zStarRating     = string.IsNullOrEmpty(tvProgram.StarRating) ? EMPTY : tvProgram.StarRatingNum.ToString() + "/8";
            string zClassification = string.IsNullOrEmpty(tvProgram.MPAARating) ? tvSchedule.TVRating : tvProgram.MPAARating;
            string zRepeat         = (tvProgram.IsRepeat(tvSchedule)) ? "Repeat" : string.Empty;

            int zRatingAge = NOTFOUND;
            if (!String.IsNullOrEmpty(zClassification))
               zRatingAge = GetRatingAge(zClassification);

            string zEpisodePart = EMPTY;
            string zGenre = EMPTY;

            if (tvSchedule.Part.Number > 0)
            {
               zEpisodePart = String.Format(
                   System.Globalization.CultureInfo.InvariantCulture, "..{0}/{1}", tvSchedule.Part.Number, tvSchedule.Part.Total);
            }
            else
            {
               zEpisodePart = EMPTY;
            }

            // MediaPortal only supports a single Genre so we use the (first) primary one
            SchedulesDirect.SoapEntities.ProgramGenre tvProgramGenres = _results.Data.Genres.ProgramGenreById(tvProgram.ID);
            if (tvProgramGenres != null && tvProgramGenres.List.Count > 0)
            {
               zGenre = tvProgramGenres.List[0].GenreClass;
            }
            else
            {
               zGenre = EMPTY;
            }

            // Add tags to description (temporary workaround until MediaPortal supports and displays more tags)
            if (!string.IsNullOrEmpty(tvProgram.Subtitle))
               description.Append(tvProgram.Subtitle).Append(": ");

            description.Append(tvProgram.Description);

            if (tvProgram.Year > 0 && _addExtraInfoToShowDescription)
               description.AppendFormat(" ({0} {1})", tvProgram.Year, tvProgram.StarRating);

            if (tvProgram.IsRepeat(tvSchedule) && _addExtraInfoToShowDescription)
            {
               if (string.IsNullOrEmpty(tvProgram.OriginalAirDateStr))
               {
                  description.Append(" (Repeat)");
               }
               else
               {
                  description.Append(" (First aired ").Append(tvProgram.OriginalAirDate.ToShortDateString()).Append(")");
               }
            }
            else if (tvProgram.IsNew(tvSchedule) && _addExtraInfoToShowDescription)
            {
               if (string.IsNullOrEmpty(tvProgram.OriginalAirDateStr))
               {
                  description.Append(" (New)");
               }
               else if (localStartTime.Subtract(tvProgram.OriginalAirDate).TotalDays > 60)
               {
                  // News may have New Attribute and OAD 8/3/1970
                  // The OAD of the "series"
                  description.Append(" (New)");
               }
               else
               {
                  description.Append(" (New: ").Append(tvProgram.OriginalAirDate.ToShortDateString()).Append(")");
               }
            }
            else if (_addExtraInfoToShowDescription)
            {
               if (!string.IsNullOrEmpty(tvProgram.OriginalAirDateStr))
               {
                  if (tvProgram.OriginalAirDate.Date == localStartTime.Date)
                     description.Append(" (New: " + tvProgram.OriginalAirDate.ToShortDateString() + ")");
                  else
                     description.Append(" (First Aired: " + tvProgram.OriginalAirDate.ToShortDateString() + ")");
               }
            }

            if (tvSchedule.HDTV && _addExtraInfoToShowDescription)
               description.Append(" (HDTV)");

            if (!string.IsNullOrEmpty(tvSchedule.Dolby) && _addExtraInfoToShowDescription)
               description.AppendFormat(" ({0})", tvSchedule.Dolby);

            if (tvProgram.Advisories != null && tvProgram.Advisories.Advisory.Count > 0 && _addExtraInfoToShowDescription)
            {
               description.AppendFormat(" ({0})", string.Join(", ", tvProgram.Advisories.Advisory.ToArray()));
            }


            // Converting Star Rating to 0-10 pre
            // http://forum.team-mediaportal.com/showpost.php?p=142866&postcount=9
            //SD Rating  <--> MP Rating
            //   1                1
            //   2                3
            //   3                4
            //   4                5
            //   5                6
            //   6                8
            //   7               10
            int mpStarRating = NOTFOUND;
            if (tvProgram.StarRatingNum == 1)
               mpStarRating = 1;
            else if (tvProgram.StarRatingNum > 1 && tvProgram.StarRatingNum <= 5)
               mpStarRating = tvProgram.StarRatingNum + 1;
            else if (tvProgram.StarRatingNum == 6)
               mpStarRating = 8;
            else if (tvProgram.StarRatingNum == 7)
               mpStarRating = 10;


            foreach (Channel pCh in channelIdList)
            {  
               bool done = false;
               if (_useTvDb && tvdb.shouldWeGetTvDbForSeries(zTitle)) {
                 try {
                   // get season/episode info from thetvdb.com
                   string sid = tvdb.getSeriesId(zTitle);
                   
                   if (sid != null || sid.Length >= 0) {
                     
                     bool allowTailMatch = true;
                     
                     // one particular hard-coded series that can't do tail-matching
                     if (zTitle.Equals("24")) {
                       allowTailMatch = false;
                     }
                     
                     string sep = tvdb.getSeasonEpisode(zTitle,sid,zEpisode,tvProgram.OriginalAirDate,allowTailMatch);
                     if (sep.Length > 0) {
                       Program mpProgram = new Program(pCh.IdChannel,localStartTime,localEndTime,zTitle,description.ToString(),zGenre,Program.ProgramState.None,tvProgram.OriginalAirDate.Date,zSeriesNum,sep,zEpisode,zEpisodePart,mpStarRating,zClassification,zRatingAge);
                     
                       UpdateProgram(mpProgram);
                       stats._iPrograms++;
                       done = true;
                     }
                   }
                 } catch (Exception ex) {
                   Log.Error("Error getting TvDb info.  Just using default info instead: {0}",ex.StackTrace);
                 }
               } 
               
               if (!done) {
                 // don't use tvdb or use it but did not find a match
                 Program mpProgram = new Program(pCh.IdChannel,localStartTime,localEndTime,zTitle,description.ToString(),zGenre,Program.ProgramState.None,tvProgram.OriginalAirDate.Date,zSeriesNum,zEpisodeNum,zEpisode,zEpisodePart,mpStarRating,zClassification,zRatingAge);
                 
                 UpdateProgram(mpProgram);
                 stats._iPrograms++;
               }
               
            }


            System.Threading.Thread.Sleep(delay);
         }
         if (_useTvDb && tvdb != null) {
           tvdb.closeCache();
         }
         return stats._iPrograms;
      }
      

      /// <summary>
      /// Clears the local channel and epg mapping cache.
      /// </summary>
      public void ClearCache()
      {
         if (_mpChannelCache != null)
            _mpChannelCache.Clear();
         if (_mpEpgMappingCache != null)
            _mpEpgMappingCache.Clear();
         if (_mpAtscChannelCache != null)
            _mpAtscChannelCache.Clear();
         if (_mpUnMappedChannelCache != null)
            _mpUnMappedChannelCache.Clear();
         if (_mpRatingAgeCache != null)
            _mpRatingAgeCache.Clear();
      }
      
      #region Protected Support Methods

      /// <summary>
      /// Builds the name of the channel based on the template.
      /// </summary>
      /// <param name="tvStation">The tv station.</param>
      /// <param name="tvStationMap">The tv station map.</param>
      /// <returns></returns>
      protected string BuildChannelName(SchedulesDirect.SoapEntities.TVStation tvStation, SchedulesDirect.SoapEntities.TVStationMap tvStationMap)
      {
         string channelName = string.Empty;
         if (tvStation != null && tvStationMap != null)
         {
            channelName = this._channelNameTemplate.ToLower(System.Globalization.CultureInfo.CurrentCulture);
            channelName = channelName.Replace("{callsign}", tvStation.CallSign);
            channelName = channelName.Replace("{name}", tvStation.Name);
            channelName = channelName.Replace("{affiliate}", tvStation.Affiliate);
            channelName = channelName.Replace("{number}", tvStationMap.Channel);
            // debug
            //channelName = channelName + " BCNbr: " + tvStation.BroadcastChannelNumber.ToString();
            //channelName = channelName + " CMajr: " + tvStationMap.ChannelMajor.ToString();
         }
         return channelName;
      }

      /// <summary>
      /// Creates a XmlTvId string using the SchedulesDirect ID + (BroadcastChannel or ChannelString) and the XMLTVID constant
      /// </summary>
      /// <param name="IsLocalBroadcast">True if the TVStation is a Local Broadcast</param>
      /// <param name="tvStation">The TVStation</param>
      /// <param name="tvStationMap">The TVStationMap</param>
      /// <returns></returns>
      protected string BuildXmlTvId(bool IsLocalBroadcast, SchedulesDirect.SoapEntities.TVStation tvStation, SchedulesDirect.SoapEntities.TVStationMap tvStationMap)
      {
         string xmlId = tvStation.ID;
         if (IsLocalBroadcast && tvStation.BroadcastChannelNumber > 0 && !tvStation.IsDigitalTerrestrial)
            xmlId += "." + tvStation.BroadcastChannelNumber.ToString() + XMLTVID;
         else
            xmlId += "." + tvStationMap.ChannelString + XMLTVID;

         return xmlId;
      }

      /// <summary>
      /// Creates a XmlTvId string using the using the Old Zap2it format and MPChannel data and the XMLTVID constant
      /// </summary>
      /// <param name="tvChannel">The MP Channel</param>
      /// <param name="zap2ItXmlTvId">The existing Zap2It based XMLTVID</param>
      /// <returns>If successful, the new SchedulesDirect format ID, else an empty string</returns>
      protected string BuildXmlTvIdFromChannelData(Channel tvChannel, string zap2ItXmlTvId)
      {
         if (String.IsNullOrEmpty(zap2ItXmlTvId))
            return String.Empty;

         if (!zap2ItXmlTvId.ToLower().EndsWith(ZAP2ITXMLTVID.ToLower()))
            return zap2ItXmlTvId;

         string[] idArr = zap2ItXmlTvId.Split(new char[] { '.' });

         string xmlId = idArr[0];

         ATSCChannel atscChannel = new ATSCChannel();
         if (GetATSCChannel(tvChannel, ref atscChannel))
            xmlId += "." + atscChannel.MajorChannel.ToString() + "-" + atscChannel.MinorChannel.ToString() + XMLTVID;
         else
         {
           IList chDetailList = (IList)tvChannel.ReferringTuningDetail();
            if (chDetailList.Count > 0)
            {
               TuningDetail chDetail = (TuningDetail)chDetailList[0];

               xmlId += "." + chDetail.ChannelNumber.ToString() + XMLTVID;
            }
         }

         return xmlId;
      }

      /// <summary>
      /// Renames the logo.
      /// </summary>
      /// <param name="oldName">The old name.</param>
      /// <param name="newName">The new name.</param>
      static protected void RenameLogo(string oldName, string newName)
      {
         //string strOldLogo = MediaPortal.Util.Utils.GetCoverArtName(MediaPortal.Util.Thumbs.TVChannel, oldName);
         //string strNewLogo = MediaPortal.Util.Utils.GetCoverArtName(MediaPortal.Util.Thumbs.TVChannel, newName);
         //if (System.IO.File.Exists(strOldLogo))
         //{
         //    try
         //    {
         //        System.IO.File.Move(strOldLogo, strNewLogo);
         //    }
         //    catch (Exception ex)
         //    {
         //        Log.Write(ex);
         //    }
         //}
      }

      /// <summary>
      /// Provides a cached wrapper for getting the channel EPG mapping.
      /// </summary>
      /// <param name="xmlTvId">The XML tv id.</param>
      /// <param name="idTvChannel">The id tv channel.</param>
      /// <param name="strTvChannel">The STR tv channel.</param>
      /// <returns>true if the mapping was found</returns>
      protected bool GetEPGMapping(string xmlTvId, out int idTvChannel, out string strTvChannel)
      {
         if (_mpEpgMappingCache.ContainsKey(xmlTvId))
         {
             object[] obj = _mpEpgMappingCache[xmlTvId] as object[];
             idTvChannel = (int)obj[0];
             strTvChannel = (string)obj[1];
             return true;
         }
         else if (LookupEPGMapping(xmlTvId, out idTvChannel, out strTvChannel))
         {
             object[] obj = new object[] { idTvChannel, strTvChannel };
             _mpEpgMappingCache.Add(xmlTvId, obj);
             return true;
         }
         return false;
      }

      /// <summary>
      /// Provides a wrapper for getting the channel by xmlTvId from the database.
      /// </summary>
      /// <param name="xmlTvId">xmlTvId aka ExternalID</param>
      /// <param name="idTvChannel">The id tv channel.</param>
      /// <param name="strTvChannel">The name tv channel.</param>
      /// <returns>true if the channel name was found</returns>
      protected bool LookupEPGMapping(string xmlTvId, out int idTvChannel, out string strTvChannel)
      {
        //Channel fch = Channel.Retrieve(
        idTvChannel  = -1;
        strTvChannel = String.Empty;

        SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof(Channel));

        sb.AddConstraint(Operator.Equals, "externalId", xmlTvId.ToString());

        SqlStatement stmt = sb.GetStatement(true);

        System.Collections.IList chList = ObjectFactory.GetCollection(typeof(Channel), stmt.Execute());

        if (chList.Count > 0)
        {
           idTvChannel  = ((Channel)chList[0]).IdChannel;
           strTvChannel = ((Channel)chList[0]).Name;
           return true;
        }

        return false;
      }

      /// <summary>
      /// Provides a cached wrapper for getting the EPG mapping for a SchedulesDirect.SoapEntities.TVSchedule.Station
      /// </summary>
      /// <param name="stationString">The SchedulesDirect.SoapEntities.TVSchedule.Station</param>
      /// <returns>List of idChannels for the Station, use GetChannelEPGMapping to get the Mapping</returns>
      protected List<Channel> GetStationChannels(string stationString)
      {
         if (_mpChannelCache == null || _mpChannelCache.Count <= 0)
           _mpChannelCache = (IList)Channel.ListAll();

         List<Channel> stList = new List<Channel>();
         foreach (Channel ch in _mpChannelCache)
         {
            if (!String.IsNullOrEmpty(ch.ExternalId))
            {
               if (ch.ExternalId.ToLower().StartsWith(stationString.ToLower().Trim() + "."))
                  stList.Add(ch);
            }
         }

         return stList;
      }

      /// <summary>
      /// Get the age assigned to the specified rating
      /// </summary>
      /// <param name="rating">rating to find the assigned age for.</param>
      /// <returns>The age assigned to the specifed rating</returns>
      protected int GetRatingAge(string rating)
      {
         int age = NOTFOUND;

         if (_mpRatingAgeCache.ContainsKey(rating.ToUpper()))
         {
            age = _mpRatingAgeCache[rating.ToUpper()];
         }
         else
         {
            if (!_mpRatingAgeCache.ContainsKey("G"))
               _mpRatingAgeCache.Add("G", Plugin.PluginSettings.RatingsAgeMPAA_G);
            if (!_mpRatingAgeCache.ContainsKey("PG"))
               _mpRatingAgeCache.Add("PG", Plugin.PluginSettings.RatingsAgeMPAA_PG);
            if (!_mpRatingAgeCache.ContainsKey("PG-13"))
               _mpRatingAgeCache.Add("PG-13", Plugin.PluginSettings.RatingsAgeMPAA_PG13);
            if (!_mpRatingAgeCache.ContainsKey("R"))
               _mpRatingAgeCache.Add("R", Plugin.PluginSettings.RatingsAgeMPAA_R);
            if (!_mpRatingAgeCache.ContainsKey("NC-17"))
               _mpRatingAgeCache.Add("NC-17", Plugin.PluginSettings.RatingsAgeMPAA_NC17);
            if (!_mpRatingAgeCache.ContainsKey("AO"))
               _mpRatingAgeCache.Add("AO", Plugin.PluginSettings.RatingsAgeMPAA_AO);
            if (!_mpRatingAgeCache.ContainsKey("NR"))
               _mpRatingAgeCache.Add("NR", Plugin.PluginSettings.RatingsAgeMPAA_NR);

            if (!_mpRatingAgeCache.ContainsKey("TV-Y"))
               _mpRatingAgeCache.Add("TV-Y", Plugin.PluginSettings.RatingsAgeTV_Y);
            if (!_mpRatingAgeCache.ContainsKey("TV-Y7"))
               _mpRatingAgeCache.Add("TV-Y7", Plugin.PluginSettings.RatingsAgeTV_Y7);
            if (!_mpRatingAgeCache.ContainsKey("TV-G"))
               _mpRatingAgeCache.Add("TV-G", Plugin.PluginSettings.RatingsAgeTV_G);
            if (!_mpRatingAgeCache.ContainsKey("TV-PG"))
               _mpRatingAgeCache.Add("TV-PG", Plugin.PluginSettings.RatingsAgeTV_PG);
            if (!_mpRatingAgeCache.ContainsKey("TV-14"))
               _mpRatingAgeCache.Add("TV-14", Plugin.PluginSettings.RatingsAgeTV_14);
            if (!_mpRatingAgeCache.ContainsKey("TV-MA"))
               _mpRatingAgeCache.Add("TV-MA", Plugin.PluginSettings.RatingsAgeTV_MA);

            if (_mpRatingAgeCache.ContainsKey(rating.ToUpper()))
            {
               age = _mpRatingAgeCache[rating.ToUpper()];
            }
            else if (!String.IsNullOrEmpty(rating))
               Log.WriteFile("Schedules Direct TV-MPAA Rating Not Found: {0}.", rating);

         }

         return age;
      }

      /// <summary>
      /// Provides a wrapper for getting the channel by Name from the database.
      /// </summary>
      /// <param name="channelName">Channel Name.</param>
      /// <param name="idTvChannel">The id tv channel.</param>
      /// <returns>true if the channel name was found</returns>
      protected bool GetChannelByName(string channelName, out int idTvChannel)
      {
         //Channel fch = Channel.Retrieve(
         idTvChannel = -1;
         SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof(Channel));

         sb.AddConstraint(Operator.Equals, "displayName", channelName.ToString());

         SqlStatement stmt = sb.GetStatement(true);

         System.Collections.IList chList = ObjectFactory.GetCollection(typeof(Channel), stmt.Execute());

         if (chList.Count > 0)
         {
            idTvChannel = ((Channel)chList[0]).IdChannel;
            return true;
         }

         return false;
      }

      /// <summary>
      /// Update the Program in the database if it exists or add if it is new.
      /// </summary>
      /// <param name="mpProgram">The Program to add to the database</param>
      protected void UpdateProgram(Program mpProgram)
      {
         IFormatProvider mmddFormat = new System.Globalization.CultureInfo(String.Empty, false);
         SqlBuilder sb = new SqlBuilder(Gentle.Framework.StatementType.Select, typeof(Program));
         // startTime < MyEndTime AND endTime > MyStartTime
         // OR ==
         string sqlW1 = String.Format("(StartTime < '{0}' and EndTime > '{1}')", mpProgram.EndTime.ToString(tvLayer.GetDateTimeString(), mmddFormat), mpProgram.StartTime.ToString(tvLayer.GetDateTimeString(), mmddFormat));
         string sqlW2 = String.Format("(StartTime = '{0}' and EndTime = '{1}')", mpProgram.StartTime.ToString(tvLayer.GetDateTimeString(), mmddFormat), mpProgram.EndTime.ToString(tvLayer.GetDateTimeString(), mmddFormat));

         sb.AddConstraint(Operator.Equals, "idChannel", mpProgram.IdChannel);
         sb.AddConstraint(string.Format("({0} or {1}) ", sqlW1, sqlW2));
         sb.AddOrderByField(true, "starttime");

         SqlStatement stmt = sb.GetStatement(true);
         IList progList = ObjectFactory.GetCollection(typeof(Program), stmt.Execute());

         if (progList.Count > 0)
         {
            bool bMatch = false;
            foreach (Program prog in progList)
            {
               if (!bMatch && prog.StartTime == mpProgram.StartTime && prog.EndTime == mpProgram.EndTime && prog.Title == mpProgram.Title)
               {
                  //update - but only allow one match
                  bMatch = true;
                  prog.Classification  = mpProgram.Classification;
                  prog.Description     = mpProgram.Description;
                  prog.EpisodeNum      = mpProgram.EpisodeNum;
                  prog.Genre           = mpProgram.Genre;
                  prog.OriginalAirDate = mpProgram.OriginalAirDate;
                  prog.SeriesNum       = mpProgram.SeriesNum;
                  prog.StarRating      = mpProgram.StarRating;
                  prog.Title           = mpProgram.Title;
                  prog.EpisodeName     = mpProgram.EpisodeName;
                  prog.Persist();
               }
               else
                  prog.Delete();
            }
            if (!bMatch)
               mpProgram.Persist();
         }
         else
         {
            mpProgram.Persist();
         }
      }

      /// <summary>
      /// Provides a cached wrapped for getting the ATSC channel.
      /// </summary>
      /// <param name="mpChannel">The TvDatabase.Channel</param>
      /// <param name="retChannel">The TvLibrary.Channels.ATSCChannel</param>
      /// <returns>true if the ATSC channel was found</returns>
      protected bool GetATSCChannel(Channel mpChannel, ref ATSCChannel retChannel)
      {
         if (_mpAtscChannelCache.ContainsKey(mpChannel.IdChannel))
         {
             retChannel = _mpAtscChannelCache[mpChannel.IdChannel] as ATSCChannel;
             return true;
         }

         System.Collections.IList tuneDetails = (System.Collections.IList)mpChannel.ReferringTuningDetail();
         if (tuneDetails.Count > 0)
         {
             if (TransFormTuningDetailToATSCChannel((TuningDetail)tuneDetails[0], ref retChannel))
             {
                _mpAtscChannelCache.Add(mpChannel.IdChannel, retChannel);
                return true;
             }
         }

         return false;
      }

      /// <summary>
      /// Fills in the ATSCChannel detail from the provided TuningDetail.
      /// </summary>
      /// <param name="tuneDetail">The TvDatabase.TuningDetail to use</param>
      /// <param name="atscChannel">The TvLibrary.Channels.ATSCChannel to fill in</param>
      /// <returns>true if successfully filled in the ATSCChannel Detail</returns>
      protected bool TransFormTuningDetailToATSCChannel(TuningDetail tuneDetail, ref ATSCChannel atscChannel)
      {
        if (tuneDetail.ChannelType != 1) //1=ATSC
           return false;

        atscChannel.MajorChannel    = tuneDetail.MajorChannel;
        atscChannel.MinorChannel    = tuneDetail.MinorChannel;
        atscChannel.PhysicalChannel = tuneDetail.ChannelNumber;
        atscChannel.FreeToAir       = tuneDetail.FreeToAir;
        atscChannel.Frequency       = tuneDetail.Frequency;
        atscChannel.IsRadio         = tuneDetail.IsRadio;
        atscChannel.IsTv            = tuneDetail.IsTv;
        atscChannel.Name            = tuneDetail.Name;
        atscChannel.NetworkId       = tuneDetail.NetworkId;
        atscChannel.PcrPid          = tuneDetail.PcrPid;
        atscChannel.PmtPid          = tuneDetail.PmtPid;
        atscChannel.Provider        = tuneDetail.Provider;
        atscChannel.ServiceId       = tuneDetail.ServiceId;
        //atscChannel.SymbolRate      = tuneDetail.Symbolrate;
        atscChannel.TransportId     = tuneDetail.TransportId;
        atscChannel.AudioPid        = tuneDetail.AudioPid;
        atscChannel.VideoPid        = tuneDetail.VideoPid;

        return true;
      }

      /// <summary>
      /// Fills in the ATSCChannel detail from the provided TuningDetail.
      /// </summary>
      /// <param name="idTvChannel">The Channel Database ID</param>
      /// <param name="station">The station</param>
      /// <param name="map">The map</param>
      /// <returns>true if a fix was completed on the map.ChannelMajor and map.ChannelMinor else false</returns>
      protected bool FixDigitalTerrestrialChannelMap(int idTvChannel, SchedulesDirect.SoapEntities.TVStation station, ref SchedulesDirect.SoapEntities.TVStationMap map)
      {
        if (station.IsDigitalTerrestrial && map.ChannelMinor <= 0)
        {
           ATSCChannel atscEPGChannel = new ATSCChannel();
           Channel mpChannel = Channel.Retrieve(idTvChannel);
           if (GetATSCChannel(mpChannel, ref atscEPGChannel))
           {
              map.ChannelMajor = atscEPGChannel.MajorChannel;
              map.ChannelMinor = atscEPGChannel.MinorChannel;

              return true;
           }
        }

        return false;
      }

      /// <summary>
      /// Finds the TV channel.
      /// </summary>
      /// <param name="station">The station.</param>
      /// <param name="map">The map.</param>
      /// <returns>TVChannel found or null</returns>
      protected Channel FindTVChannel(SchedulesDirect.SoapEntities.TVStation station, SchedulesDirect.SoapEntities.TVStationMap map, bool LineupIsLocalBroadcast)
      {
         string strTvChannel;
         int idTvChannel;

         /*
         Log.WriteFile("Attempting Channel Find/Match for: Callsign: {0}, Map Channel: {1}, Map Major {2}, Map Minor: {3}, Map StationID: {4} ", 
            station.CallSign, map.Channel, map.ChannelMajor, map.ChannelMinor, map.StationId);
         */

         string xmlTvId = BuildXmlTvId(LineupIsLocalBroadcast, station, map);

         #region Check if the channel is already in EPG mapping database
         //Log.WriteFile("GetEPGMapping: {0} for {1}", station.Name, station.ID + XMLTVID);
         if (GetEPGMapping(xmlTvId, out idTvChannel, out strTvChannel))
         {
            Log.WriteFile("Channel {0} was found as {1} in EPG mapping database", station.Name, strTvChannel);
            FixDigitalTerrestrialChannelMap(idTvChannel, station, ref map);

            return Channel.Retrieve(idTvChannel);
         }
         #endregion

         #region Try locating the channel by callsign
         //Log.WriteFile("GetChannelByName: {0}", station.CallSign);
         if (GetChannelByName(station.CallSign, out idTvChannel) == true)
         {
            Channel mpChannel = Channel.Retrieve(idTvChannel);
            Log.WriteFile("Matched channel {0} to {1} using CallSign", station.CallSign, mpChannel.Name);
            FixDigitalTerrestrialChannelMap(idTvChannel, station, ref map);
            
            return mpChannel;
         }
         #endregion

         #region Iterate through each channel looking for a match
         foreach (Channel mpChannel in _mpChannelCache)
         {

            // Get the TuningDetail for the Channel
            System.Collections.IList chDetailList = (System.Collections.IList)mpChannel.ReferringTuningDetail();
            if (chDetailList.Count <= 0)
               continue;
            TuningDetail chDetail = (TuningDetail)chDetailList[0];


            // Only look at non-mapped channels
            if (String.IsNullOrEmpty(mpChannel.ExternalId)) 
            {

               // Check for an ATSC major/minor channel number match
               ATSCChannel atscChannel = new ATSCChannel();
               if (LineupIsLocalBroadcast && GetATSCChannel(mpChannel, ref atscChannel))
               {
                  if (map.ChannelMajor == atscChannel.MajorChannel && map.ChannelMinor == atscChannel.MinorChannel)
                  {
                     Log.WriteFile("Matched channel {0} to {1} by ATSC channel ({2}-{3})",
                         station.CallSign, mpChannel.Name, atscChannel.MajorChannel, atscChannel.MinorChannel);

                     return mpChannel;
                  }
               }
               // If the Lineup is a LocalBroadcast we want to give preference to 
               // searching by Broadcast Number
               // else give preference to the Major Channel Number
               else if (LineupIsLocalBroadcast && !station.IsDigitalTerrestrial)
               {
                  // Not an ATSC channel so check for an over-the-air 
                  //(by checking it has a frequency) broadcast channel number match
                  if (chDetail.ChannelNumber == station.BroadcastChannelNumber)
                  {
                     if (chDetail.Frequency != 0 || _allowChannelNumberMapping)
                     {
                        Log.WriteFile("Matched channel {0} to {1} by OTA broadcast channel ({2})",
                           station.CallSign, chDetail.Name, chDetail.ChannelNumber);

                        return mpChannel;
                     }
                  }
               }
               else if (!LineupIsLocalBroadcast)
               {
                  // Check for an over-the-air (by checking it has a frequency) 
                  // major channel number match
                  if (chDetail.ChannelNumber == map.ChannelMajor)
                  {
                     if (chDetail.Frequency != 0 || _allowChannelNumberMapping)
                     {
                        Log.WriteFile("Matched channel {0} to {1} by lineup channel ({2})",
                           station.CallSign, chDetail.Name, chDetail.ChannelNumber);

                        return mpChannel;
                     }
                  }
               }
            }
         }
         #endregion

         #region Check the UnMapped Channels if the lineup has changed
         foreach (Channel mpChannel in _mpUnMappedChannelCache)
         {
            string[] xIds = mpChannel.ExternalId.Split(new char[] { '.' });
            string[] cIds = xmlTvId.Split(new char[] { '.' });

            if (xIds[1].ToLower() == cIds[1].ToLower())
            {
               _mpUnMappedChannelCache.Remove(mpChannel);
               return mpChannel;
            }
         }
         #endregion

         #region Iterate through each channel again looking @ MajorChannel Number even if it is a local broadcast
         foreach (Channel mpChannel in _mpChannelCache)
         {
            if (!String.IsNullOrEmpty(mpChannel.ExternalId))
               continue;

            System.Collections.IList chDetailList = (System.Collections.IList)mpChannel.ReferringTuningDetail();
            if (chDetailList.Count <= 0)
               continue;
            TuningDetail chDetail = (TuningDetail)chDetailList[0];

            if (LineupIsLocalBroadcast && !station.IsDigitalTerrestrial && chDetail.Frequency != 0) 
            {
               // Check for an over-the-air (by checking it has a frequency) major channel number match
               if (chDetail.ChannelNumber == map.ChannelMajor)
               {
                  Log.WriteFile("Matched channel {0} to {1} by lineup channel ({2})",
                     station.CallSign, chDetail.Name, chDetail.ChannelNumber);

                  return mpChannel;
               }
            }
            else if (!LineupIsLocalBroadcast)
            {
               // Not an ATSC channel so check for an over-the-air (by checking it has a frequency) broadcast channel number match
               if (chDetail.ChannelNumber == station.BroadcastChannelNumber)
               {
                  Log.WriteFile("Matched channel {0} to {1} by OTA broadcast channel ({2})",
                     station.CallSign, chDetail.Name, chDetail.ChannelNumber);

                  return mpChannel;
               }
            }

         }
         #endregion

         return null;
      }

      /// <summary>
      /// Sorts the TV channels.
      /// </summary>
      protected void SortTVChannels()
      {
         // Get a fresh list of channels
         _mpChannelCache = (IList)Channel.ListAll();

         if (_mpChannelCache == null)
            return;

         if (_mpChannelCache.Count <= 0)
            return;

         List<ChannelInfo> listChannels = new List<ChannelInfo>();
         foreach (Channel mpChannel in _mpChannelCache)
         {
            ChannelInfo chi;
            ATSCChannel atscCh = new ATSCChannel();
            if (GetATSCChannel(mpChannel, ref atscCh))
            {
               chi = new ChannelInfo(mpChannel.IdChannel, atscCh.MajorChannel, atscCh.MinorChannel, mpChannel.Name);
            }
            else
            {
              System.Collections.IList chDetailList = (System.Collections.IList)mpChannel.ReferringTuningDetail();
               if (chDetailList.Count <= 0)
                  continue;
               TuningDetail chDetail = (TuningDetail)chDetailList[0];

               chi = new ChannelInfo(mpChannel.IdChannel, chDetail.ChannelNumber, 0, mpChannel.Name);
            }

            listChannels.Add(chi);
         }

         ChannelSorter sorter = new ChannelSorter(listChannels, new ChannelNumberComparer());

         for (int i = 0; i < listChannels.Count; i++)
         {
            ChannelInfo sChi = listChannels[i];
            foreach (Channel mpChannel in _mpChannelCache)
            {
               if (sChi.ID != mpChannel.IdChannel)
                  continue;

               if (mpChannel.SortOrder != i)
               {
                  mpChannel.SortOrder = i;
                  mpChannel.Persist();
               }
            }
         }

      }
  
      #endregion

      #region IDisposable
      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
             ClearCache();
             if (this._results != null)
                this._results = null;
         }
      }
      #endregion
   }

   #region ImportStats Class

   public class ImportStats
   {
      internal string _status = string.Empty;
      internal int _iPrograms;
      internal int _iChannels;
      internal DateTime _startTime = DateTime.Now;
      internal DateTime _endTime = DateTime.MinValue;

      /// <summary>
      /// Gets the status.
      /// </summary>
      /// <value>The status.</value>
      public string Status
      {
         get { return _status; }
      }
      /// <summary>
      /// Gets the programs.
      /// </summary>
      /// <value>The programs.</value>
      public int Programs
      {
         get { return _iPrograms; }
      }
      /// <summary>
      /// Gets the channels.
      /// </summary>
      /// <value>The channels.</value>
      public int Channels
      {
         get { return _iChannels; }
      }
      /// <summary>
      /// Gets the start time.
      /// </summary>
      /// <value>The start time.</value>
      public DateTime StartTime
      {
         get { return _startTime; }
      }
      /// <summary>
      /// Gets the end time.
      /// </summary>
      /// <value>The end time.</value>
      public DateTime EndTime
      {
         get { return _endTime; }
      }
   };
   #endregion


   
}
