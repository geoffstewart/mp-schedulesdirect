Source: http://code.google.com/p/mp-schedulesdirect/
SVN Version: r18
Built: August 19, 2010
MP Verstion: 1.1.0 Final

Description:
This version restores behaviour with SchedulesDirect after the service interruption in mid-July 2010.  

Also, this version will be a bit more aggressive to fill in the episode information for scheduled recordings.

Be sure to set your recording naming convetion to include the episode information so MyTVSeries can automatically figure out what episodes are recorded:
 
For example:
  %title%\%title% - %episode% - %name%

See the wiki for more information and provide feedback in the mp forums.



Installation:

Stop TV Service.

Put both DLL files in C:\Program Files\Team MediaPortal\MediaPortal TV Server\Plugins

SchedulesDirectPluginTVE3.dll
TvdbLib.dll

Start TV Service.
