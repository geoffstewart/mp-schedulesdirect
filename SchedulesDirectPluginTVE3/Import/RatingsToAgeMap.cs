using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulesDirect.Import
{
  public static class RatingsToAgeMap
  {
    private static Dictionary<string, int> _ratingMap = new Dictionary<string, int>(13);

    /// <summary>
    /// Initializes the <see cref="T:RatingsToAgeMap"/> class.
    /// </summary>
    static RatingsToAgeMap()
    {
      _ratingMap.Add("G", Plugin.PluginSettings.RatingsAgeMPAA_G);
      _ratingMap.Add("PG", Plugin.PluginSettings.RatingsAgeMPAA_PG);
      _ratingMap.Add("PG-13", Plugin.PluginSettings.RatingsAgeMPAA_PG13);
      _ratingMap.Add("R", Plugin.PluginSettings.RatingsAgeMPAA_R);
      _ratingMap.Add("NC-17", Plugin.PluginSettings.RatingsAgeMPAA_NC17);
      _ratingMap.Add("AO", Plugin.PluginSettings.RatingsAgeMPAA_AO);
      _ratingMap.Add("NR", Plugin.PluginSettings.RatingsAgeMPAA_NR);

      _ratingMap.Add("TV-Y", Plugin.PluginSettings.RatingsAgeTV_Y);
      _ratingMap.Add("TV-Y7", Plugin.PluginSettings.RatingsAgeTV_Y7);
      _ratingMap.Add("TV-G", Plugin.PluginSettings.RatingsAgeTV_G);
      _ratingMap.Add("TV-PG", Plugin.PluginSettings.RatingsAgeTV_PG);
      _ratingMap.Add("TV-14", Plugin.PluginSettings.RatingsAgeTV_14);
      _ratingMap.Add("TV-MA", Plugin.PluginSettings.RatingsAgeTV_MA);
    }

    /// <summary>
    /// Gets the age for the specified rating.
    /// </summary>
    /// <param name="rating">The rating to find.</param>
    /// <param name="age">The age asigned to the rating.</param>
    /// <returns>true if found, else false</returns>
    public static bool GetAgeForRating(string rating, out int age)
    {
      return _ratingMap.TryGetValue(rating, out age);
    }

  }
}
