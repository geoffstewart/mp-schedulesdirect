using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using SetupTv;
using System.Reflection;

namespace SchedulesDirect.Plugin
{
  public partial class SchedulesDirectPluginConfig : SetupTv.SectionSettings
  {
    TvLibrary.CountryCollection countryList;

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="T:SchedulesDirectPluginConfig"/> class.
    /// </summary>
    public SchedulesDirectPluginConfig()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:SchedulesDirectPluginConfig"/> class.
    /// </summary>
    public SchedulesDirectPluginConfig(string name)
      : base(name)
    {
      InitializeComponent();

      textBoxUsername.Validating += new CancelEventHandler(textBoxUsername_Validating);
      textBoxPassword.Validating += new CancelEventHandler(textBoxPassword_Validating);
      comboBoxNameFormat.Validating += new CancelEventHandler(comboBoxNameFormat_Validating);

      textBoxMPAARatingG.Validating += new CancelEventHandler(textBoxMPAARatingG_Validating);
      textBoxMPAARatingPG.Validating += new CancelEventHandler(textBoxMPAARatingPG_Validating);
      textBoxMPAARatingPG13.Validating += new CancelEventHandler(textBoxMPAARatingPG13_Validating);
      textBoxMPAARatingR.Validating += new CancelEventHandler(textBoxMPAARatingR_Validating);
      textBoxMPAARatingNC17.Validating += new CancelEventHandler(textBoxMPAARatingNC17_Validating);
      textBoxMPAARatingAO.Validating += new CancelEventHandler(textBoxMPAARatingAO_Validating);
      textBoxMPAARatingNR.Validating += new CancelEventHandler(textBoxMPAARatingNR_Validating);

      textBoxTVRatingY.Validating += new CancelEventHandler(textBoxTVRatingY_Validating);
      textBoxTVRatingY7.Validating += new CancelEventHandler(textBoxTVRatingY7_Validating);
      textBoxTVRatingG.Validating += new CancelEventHandler(textBoxTVRatingG_Validating);
      textBoxTVRatingPG.Validating += new CancelEventHandler(textBoxTVRatingPG_Validating);
      textBoxTVRating14.Validating += new CancelEventHandler(textBoxTVRating14_Validating);
      textBoxTVRatingMA.Validating += new CancelEventHandler(textBoxTVRatingMA_Validating);
      
      buttonAddMapping.Click += new EventHandler(buttonAddMapping_Click);
      
    }
    #endregion

    #region Section Activated/Deactiviated Overrides
    public override void OnSectionDeActivated()
    {

      PluginSettings.UseDvbEpgGrabber = false;
      PluginSettings.Username = textBoxUsername.Text;
      PluginSettings.Password = textBoxPassword.Text;
      PluginSettings.GuideDays = (int)numericUpDownDays.Value;
      PluginSettings.AddNewChannels = checkBoxAddChannels.Checked;
      PluginSettings.RenameExistingChannels = checkBoxRenameChannels.Checked;
      PluginSettings.ChannelNameTemplate = comboBoxNameFormat.Text;
      PluginSettings.NotifyOnCompletion = checkBoxNotification.Checked;
      PluginSettings.ExternalInput = (TvLibrary.Implementations.AnalogChannel.VideoInputType)Enum.Parse(typeof(TvLibrary.Implementations.AnalogChannel.VideoInputType), comboBoxExternalInput.SelectedItem.ToString());
      PluginSettings.AllowChannelNumberOnlyMapping = checkBoxAllowChannelNumberOnlyMapping.Checked;
      PluginSettings.ExternalInputCountry = countryList.GetTunerCountry(comboBoxExternalInputCountry.SelectedItem.ToString());
      PluginSettings.SortChannelsByNumber = checkBoxSortChannelsByChannelNumber.Checked;
      PluginSettings.DeleteChannelsWithNoEPGMapping = checkBoxDeleteChannelsWithNoEPGMapping.Checked;
      PluginSettings.AddExtraDataToShowDescription = checkBoxAddExtraDataToShowDescription.Checked;
      PluginSettings.RemapChannelsOnLineupChange = checkBoxRemapChannelsOnLineupChange.Checked;
      PluginSettings.AddAnalogChannels = checkBoxAddAnalogChannels.Checked;

      if (radioButtonUpdateLatestHours36.Checked == true && PluginSettings.GuideDays > 1)
        PluginSettings.LastMinuteGuideHours = 36;
      else if (radioButtonUpdateLatestHours48.Checked == true && PluginSettings.GuideDays > 2)
        PluginSettings.LastMinuteGuideHours = 48;
      else if (radioButtonUpdateLatestHours72.Checked == true && PluginSettings.GuideDays > 3)
        PluginSettings.LastMinuteGuideHours = 72;
      else
        PluginSettings.LastMinuteGuideHours = 24;

      PluginSettings.RatingsAgeMPAA_G    = RatingParse(textBoxMPAARatingG.Text);
      PluginSettings.RatingsAgeMPAA_PG   = RatingParse(textBoxMPAARatingPG.Text);
      PluginSettings.RatingsAgeMPAA_PG13 = RatingParse(textBoxMPAARatingPG13.Text);
      PluginSettings.RatingsAgeMPAA_R    = RatingParse(textBoxMPAARatingR.Text);
      PluginSettings.RatingsAgeMPAA_NC17 = RatingParse(textBoxMPAARatingNC17.Text);
      PluginSettings.RatingsAgeMPAA_AO   = RatingParse(textBoxMPAARatingAO.Text);
      PluginSettings.RatingsAgeMPAA_NR   = RatingParse(textBoxMPAARatingNR.Text);

      PluginSettings.RatingsAgeTV_Y  = RatingParse(textBoxTVRatingY.Text);
      PluginSettings.RatingsAgeTV_Y7 = RatingParse(textBoxTVRatingY7.Text);
      PluginSettings.RatingsAgeTV_G  = RatingParse(textBoxTVRatingG.Text);
      PluginSettings.RatingsAgeTV_PG = RatingParse(textBoxTVRatingPG.Text);
      PluginSettings.RatingsAgeTV_14 = RatingParse(textBoxTVRating14.Text);
      PluginSettings.RatingsAgeTV_MA = RatingParse(textBoxTVRatingMA.Text);


      if (checkboxForceUpdate.Checked)
      {
        PluginSettings.NextPoll = DateTime.Now;
      }
      
      //tvdb props
      PluginSettings.UseTvDb = checkBoxUseTvDb.Checked;
      PluginSettings.TvDbLogDebug = checkBoxLogDebug.Checked;
      
      Hashtable ht = new Hashtable();
      foreach(string m in listBoxSeriesMapping.Items) {
        string[] sp = m.Split('|');
        if (sp.Length == 2) {
          ht.Add(sp[0],sp[1]);
        }
      }
      PluginSettings.TvDbSeriesMappings = ht;
      
     

      PluginSettings.TvDbLibCache = textBoxTvDbLibCache.Text;
      
      base.OnSectionDeActivated();
    }

    public override void OnSectionActivated()
    {
      countryList = new TvLibrary.CountryCollection();

      this.Text = PluginSetup.PLUGIN_NAME + " v" + PluginSetup.PLUGIN_VERSION;
      this.textBoxUsername.Text = PluginSettings.Username;
      this.textBoxPassword.Text = PluginSettings.Password;
      this.numericUpDownDays.Value = PluginSettings.GuideDays;
      this.checkBoxAddChannels.Checked = PluginSettings.AddNewChannels;
      this.checkBoxRenameChannels.Checked = PluginSettings.RenameExistingChannels;
      this.comboBoxNameFormat.Text = PluginSettings.ChannelNameTemplate;
      this.checkBoxNotification.Checked = PluginSettings.NotifyOnCompletion;

      this.comboBoxExternalInput.Items.Clear();
      this.comboBoxExternalInput.Items.AddRange(Enum.GetNames(typeof(TvLibrary.Implementations.AnalogChannel.VideoInputType)));
      this.comboBoxExternalInput.SelectedIndex = this.comboBoxExternalInput.FindStringExact(PluginSettings.ExternalInput.ToString());
      this.comboBoxExternalInput.Enabled = this.checkBoxAddChannels.Checked;

      this.comboBoxExternalInputCountry.Items.Clear();
      this.comboBoxExternalInputCountry.Items.AddRange(countryList.Countries);
      this.comboBoxExternalInputCountry.SelectedIndex = this.comboBoxExternalInputCountry.FindStringExact(PluginSettings.ExternalInputCountry.ToString());
      this.comboBoxExternalInputCountry.Enabled = this.checkBoxAddChannels.Checked;

      this.checkBoxAllowChannelNumberOnlyMapping.Checked = PluginSettings.AllowChannelNumberOnlyMapping;
      this.checkBoxSortChannelsByChannelNumber.Checked = PluginSettings.SortChannelsByNumber;
      this.checkBoxDeleteChannelsWithNoEPGMapping.Checked = PluginSettings.DeleteChannelsWithNoEPGMapping;

      this.checkBoxAddExtraDataToShowDescription.Checked = PluginSettings.AddExtraDataToShowDescription;
      this.checkBoxRemapChannelsOnLineupChange.Checked = PluginSettings.RemapChannelsOnLineupChange;
      this.checkBoxAddAnalogChannels.Checked = PluginSettings.AddAnalogChannels;

      if (PluginSettings.LastMinuteGuideHours == 36)
        radioButtonUpdateLatestHours36.Checked = true;
      else if (PluginSettings.LastMinuteGuideHours == 48)
        radioButtonUpdateLatestHours48.Checked = true;
      else if (PluginSettings.LastMinuteGuideHours == 72)
        radioButtonUpdateLatestHours72.Checked = true;
      else
        radioButtonUpdateLatestHours24.Checked = true;

      textBoxMPAARatingG.Text = PluginSettings.RatingsAgeMPAA_G.ToString();
      textBoxMPAARatingPG.Text = PluginSettings.RatingsAgeMPAA_PG.ToString();
      textBoxMPAARatingPG13.Text = PluginSettings.RatingsAgeMPAA_PG13.ToString();
      textBoxMPAARatingR.Text = PluginSettings.RatingsAgeMPAA_R.ToString();
      textBoxMPAARatingNC17.Text = PluginSettings.RatingsAgeMPAA_NC17.ToString();
      textBoxMPAARatingAO.Text = PluginSettings.RatingsAgeMPAA_AO.ToString();
      textBoxMPAARatingNR.Text = PluginSettings.RatingsAgeMPAA_NR.ToString();

      textBoxTVRatingY.Text = PluginSettings.RatingsAgeTV_Y.ToString();
      textBoxTVRatingY7.Text = PluginSettings.RatingsAgeTV_Y7.ToString();
      textBoxTVRatingG.Text = PluginSettings.RatingsAgeTV_G.ToString();
      textBoxTVRatingPG.Text = PluginSettings.RatingsAgeTV_PG.ToString();
      textBoxTVRating14.Text = PluginSettings.RatingsAgeTV_14.ToString();
      textBoxTVRatingMA.Text = PluginSettings.RatingsAgeTV_MA.ToString();

      // thetvdb.com
      checkBoxUseTvDb.Checked = PluginSettings.UseTvDb;
      checkBoxLogDebug.Checked = PluginSettings.TvDbLogDebug;
      
      listBoxSeriesMapping.Items.Clear();
      
      Hashtable ht = PluginSettings.TvDbSeriesMappings;
      foreach(string k in ht.Keys) {
        string v = (string)ht[k];
        listBoxSeriesMapping.Items.Add(k + "|" + v);
      }

      textBoxTvDbLibCache.Text = PluginSettings.TvDbLibCache;
    }
    #endregion

    #region Main Tab
    /// <summary>
    /// Handles the Validating event of the textBoxPassword control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
    private void textBoxPassword_Validating(object sender, CancelEventArgs e)
    {
      if (textBoxPassword.Text.Length < 6)
      {
        errorProvider.SetError(textBoxPassword, "Password must be at least 6 characters");
        e.Cancel = true;
      }
      else
      {
        errorProvider.SetError(textBoxPassword, string.Empty);
      }
    }

    /// <summary>
    /// Handles the Validating event of the textBoxUsername control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
    private void textBoxUsername_Validating(object sender, CancelEventArgs e)
    {
      if (textBoxUsername.Text.Length < 6)
      {
        errorProvider.SetError(textBoxUsername, "Username must be at least 6 characters");
        e.Cancel = true;
      }
      else
      {
        errorProvider.SetError(textBoxUsername, string.Empty);
      }
    }

    private void linkSchedulesDirect_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      System.Diagnostics.Process.Start("http://www.schedulesdirect.org");
    }
    #endregion

    #region Advanced Tab
    /// <summary>
    /// Handles the Validating event of the comboBoxNameFormat control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="T:System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
    private void comboBoxNameFormat_Validating(object sender, CancelEventArgs e)
    {
      if (comboBoxNameFormat.Text.IndexOf("{") > -1 && comboBoxNameFormat.Text.IndexOf("}") > -1)
      {
        errorProvider.SetError(comboBoxNameFormat, string.Empty);
      }
      else
      {
        errorProvider.SetError(comboBoxNameFormat, "Invalid channel name format");
        e.Cancel = true;
      }
    }

    /// <summary>
    /// Handles the CheckedChanged event of the checkBoxAddChannels control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
    private void checkBoxAddChannels_CheckedChanged(object sender, EventArgs e)
    {
      comboBoxExternalInput.Enabled = checkBoxAddChannels.Checked;
      if (!checkBoxAddChannels.Checked)
        checkBoxAddAnalogChannels.Checked = false;

      checkBoxAddAnalogChannels.Enabled = checkBoxAddChannels.Checked;
    }
    #endregion

    #region Ratings Validating
    void textBoxRating_Validating(TextBox textBox, CancelEventArgs e)
    {
      if (textBox.Text.Length < 1)
      {
        errorProvider.SetError(textBox, "The Rating must have a numeric value");
        e.Cancel = true;
      }
      else if (!IsNumeric(textBox.Text))
      {
        errorProvider.SetError(textBox, "The Rating must have a numeric value");
        e.Cancel = true;
      }
      else
      {
        errorProvider.SetError(textBox, string.Empty);
      }
    }
    #endregion

    #region TVRating Validating
    void textBoxTVRatingMA_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRatingMA, e);
    }

    void textBoxTVRating14_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRating14, e);
    }

    void textBoxTVRatingPG_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRatingPG, e);
    }

    void textBoxTVRatingG_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRatingG, e);
    }

    void textBoxTVRatingY7_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRatingY7, e);
    }

    void textBoxTVRatingY_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxTVRatingY, e);
    }
    #endregion

    #region MPAA Rating Validating
    void textBoxMPAARatingNR_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingNR, e);
    }

    void textBoxMPAARatingAO_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingAO, e);
    }

    void textBoxMPAARatingNC17_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingNC17, e);
    }

    void textBoxMPAARatingR_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingR, e);
    }

    void textBoxMPAARatingPG13_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingPG13, e);
    }

    void textBoxMPAARatingPG_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingPG, e);
    }

    void textBoxMPAARatingG_Validating(object sender, CancelEventArgs e)
    {
      textBoxRating_Validating(textBoxMPAARatingG, e);
    }
    #endregion

    int RatingParse(string rating)
    {
      int retVal = 0;
      try
      {
        retVal = Int32.Parse(rating);
      }
      catch { }

      return retVal;
    }

    #region IsNumeric
    // IsNumeric Function
    static bool IsNumeric(object Expression)
    {
      // Variable to collect the Return value of the TryParse method.
      bool isNum;

      // Define variable to collect out parameter of the TryParse method. If the conversion fails, the out parameter is zero.
      double retNum;

      // The TryParse method converts a string in a specified style and culture-specific format to its double-precision floating point number equivalent.
      // The TryParse method does not generate an exception if the conversion fails. If the conversion passes, True is returned. If it does not, False is returned.
      isNum = Double.TryParse(Convert.ToString(Expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
      return isNum;
    }
    #endregion

    private void checkBoxUseTvDb_CheckedChanged(object sender, EventArgs e)
    {

    }

    private void buttonAddMapping_Click(object sender, EventArgs e)
    {
      string sdName = textBoxFromMapping.Text;
      string tvDbName = textBoxToMapping.Text;
      
      if (sdName == null || sdName.Length == 0) {
        //errorProvider.SetError(textBoxFromMapping,"SchedulesDirect Name must be filled in");
        MessageBox.Show("SchedulesDirect Name must be filled in","Error",MessageBoxButtons.OK);
        
        return;
      }
      if (tvDbName == null || tvDbName.Length == 0) {
        //errorProvider.SetError(textBoxToMapping,"thetvdb.com Name must be filled in");
        MessageBox.Show("thetvdb.com Name must be filled in","Error",MessageBoxButtons.OK);
        return;
      }
      string tvdbPrefix="";
      if (checkBoxSeriesId.Checked) {
         tvdbPrefix = "id=";
      }
      
      string mapping = sdName + "|" + tvdbPrefix + tvDbName;
      listBoxSeriesMapping.Items.Add(mapping);
    }
    
    private void buttonDeleteMapping_Click(object sender, EventArgs e)
    {
      Object currSel = listBoxSeriesMapping.Items[(listBoxSeriesMapping.SelectedIndex)];
      // remove it
      listBoxSeriesMapping.Items.Remove(currSel);
      
    }
    
  


    private void textBoxTvDbLibCache_TextChanged(object sender, EventArgs e)
    {
      string myDir = textBoxTvDbLibCache.Text;
      if (!Directory.Exists(myDir)) {
        try {
          Directory.CreateDirectory(myDir);
          
        } catch (Exception ex) {
          MessageBox.Show("Unable to create the TvDbLib Cache directory.  Try creating it manually. " + ex.Message,"Error",MessageBoxButtons.OK);
          return;
        }
        
      }
    }
  }
}