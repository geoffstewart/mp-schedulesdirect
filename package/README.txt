Source: http://code.google.com/p/mp-schedulesdirect/
SVN Version: r24
Built: January 21, 2011
MP Verstion: SVN 27297

Description:
Added the ability to specify the tvdb.com series by ID... Use the check box to have it use the name field as an ID.

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
