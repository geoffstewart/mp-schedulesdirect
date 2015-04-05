# SchedulesDirect Plugin for MediaPortal #

This plugin for **MediaPortal** allows users to populate their Electronic Program Guide (EPG) with data from **SchedulesDirect**

## News ##
I have switched to using ForTheRecord as my scheduling tool within MediaPortal.  I have created a similar tool for that setup called GuideEnricher (http://code.google.com/p/ftr-guide-enhancer/).

I am still maintaining this tool as well.

## Currently available for download ##
**1.2.3.2**
  * May 7, 2012 - added a feature where you can specify the audio input for channels that the plugin adds.  Also, this version uses a MPE1 installer (thanks kilik360 for the suggestions).
**MP1.2.3**
  * Apr 28, 2012 - Simply rebuilt using the libraries for MP 1.2.3
**svn26**
  * This version is the latest for the 1.2 SVN loads.  It features support for the plugin compatibility that is now present in MP 1.2.   Thanks again to ajp for the load
**svn24**
  * This version is for folks using the 1.2 SVN loads.  There was a change to the MP libraries that required a change.  Thanks to ajp for this load!
**svn21**
  * This version adds the ability to specify the tvdb.com by series ID.  In the setuptv.exe tool, you can use the checkbox in the mapping and in the tvdb.com field, enter the ID found on the website for the series in question.
  * For "$..! My Dad Says", you would put "$..! My Dad Says" in the Schedules Direct name, and 164951 in the tvdb.com field as well as check the box that says Series ID before adding the mapping.  The ID is found in the URL when you're looking at the series on thetvdb.com website.

**svn18**
  * The current source from the forum compile for MP1.1.0 Final
  * The plugin supports searching thetvdb.com based on series name and episode name to find the Season Number and Episode Number (SxxExx)
  * With the SxxExx data in your EPG, now you can name your recordings with this information so that TVSeries can automatically pick up new recordings.


# Workaround #
Oct 4, 2010:

  * Trouble getting episodes of Chuck to work for you?  Me too.  There is a workaround described in this Issue: https://code.google.com/p/mp-schedulesdirect/issues/detail?id=1&can=1


---
