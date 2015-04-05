
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Serialization;
using System.Net;
using System.Web.Services;
using System.Web.Services.Protocols;

using TvLibrary.Log;

namespace SchedulesDirect
{
  /// <summary>
  /// SchedulesDirectWebService provides access to the EPG listing data at http://schedulesdirect.org/
  /// </summary>
  public class SchedulesDirectWebService : IDisposable
  {
    private SchedulesDirectWebServiceProxy _proxy;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:SchedulesDirectWebService"/> class.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public SchedulesDirectWebService(string username, string password)
    {
      // Instantiate a proxy class that will do the real communications
      _proxy = new SchedulesDirectWebServiceProxy(username, password);
    }

    /// <summary>
    /// Downloads data from SchedulesDirect for the specified start and end times and
    /// returns a SchedulesDirectDownloadResults structure containing the data XmlNode tree,
    /// the subscription expiration date and a list any other messages returned
    /// by SchedulesDirect.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time.</param>
    /// <returns>SchedulesDirectDownloadResults</returns>
    public SchedulesDirect.SoapEntities.DownloadResults Download(DateTime startTime, DateTime endTime)
    {
      XmlNode[] xmlNodes = (XmlNode[])_proxy.download(ConvertDateTime(startTime), ConvertDateTime(endTime));
      return TransformResults(xmlNodes);
    }

    /// <summary>
    /// Begins the downloads data from SchedulesDirect for the specified start and end times asyncronously
    /// </summary>
    /// <param name="startTime">The start time.</param>
    /// <param name="endTime">The end time.</param>
    /// <param name="callback">The callback.</param>
    /// <param name="asyncState">State of the async.</param>
    /// <returns>IAsyncResult</returns>
    public IAsyncResult BeginDownload(DateTime startTime, DateTime endTime, AsyncCallback callback, object asyncState)
    {
      return _proxy.Begindownload(ConvertDateTime(startTime), ConvertDateTime(endTime), callback, asyncState);
    }

    /// <summary>
    /// Returns the result of BeginDownload when the asyncrhonous process completes
    /// </summary>
    /// <param name="asyncResult">The async result.</param>
    /// <returns>SchedulesDirectDownloadResults</returns>
    public SchedulesDirect.SoapEntities.DownloadResults EndDownload(IAsyncResult asyncResult)
    {
      XmlNode[] xmlNodes = (XmlNode[])_proxy.Enddownload(asyncResult);
      return TransformResults(xmlNodes);
    }

    /// <summary>
    /// Gets the suggested time for the next data poll. Defaults to 24 hours from now
    /// if no sane value (between 1 and 24 hours from now) is provided.
    /// </summary>
    /// <returns>DateTime of the next suggested poll</returns>
    public DateTime GetSuggestedTime()
    {
      DateTime suggestedTime = DateTime.MinValue;

      // The next polling time is contained in the result from acknowledge() call
      XmlNode[] result = (XmlNode[])_proxy.acknowledge();
      if (result != null)
      {
        foreach (XmlNode node in result)
        {
          switch (node.Name)
          {
            case "suggestedTime":
              // Try to parse the time, suggestedTime will be set to DateTime.MinValue on failure
              DateTime.TryParse(node.InnerText, out suggestedTime);
              break;
          }
        }
      }

      // Default to 24 hours from now if we can't get a sane value
      if (suggestedTime >= DateTime.Now.AddHours(24))
      {
        suggestedTime = DateTime.Now.AddHours(24);
      }

      return suggestedTime;
    }

    /// <summary>
    /// Transforms the results from XML nodes to DownloadResults entity.
    /// </summary>
    /// <param name="xmlNodes">The XML nodes.</param>
    /// <returns>a DownloadResults entity</returns>
    private SchedulesDirect.SoapEntities.DownloadResults TransformResults(XmlNode[] xmlNodes)
    {
      SchedulesDirect.SoapEntities.DownloadResults downloadResults = new SchedulesDirect.SoapEntities.DownloadResults();

      if (xmlNodes == null)
        return downloadResults;

      foreach (XmlNode node in xmlNodes)
      {
        switch (node.Name)
        {
          case "messages":
            downloadResults._messages = new List<string>();
            foreach (string msg in node.InnerText.Trim().Split('\n'))
            {
              if (msg.StartsWith("Your subscription will expire:"))
              {
                DateTime.TryParse(msg.Substring(31), out downloadResults._subscriptionExpiration);
              }
              else if (!string.IsNullOrEmpty(msg))
              {
                downloadResults._messages.Add(msg.Trim());
              }
            }
            break;
          case "xtvd":
            XmlSerializer serializer = new XmlSerializer(typeof(SchedulesDirect.SoapEntities.XTVD));
            downloadResults._data = (SchedulesDirect.SoapEntities.XTVD)serializer.Deserialize(new XmlNodeReader(node));
            break;
        }
      }
      return downloadResults;
    }

    /// <summary>
    /// Converts the date time.
    /// </summary>
    /// <param name="dateTime">The original DateTime.</param>
    /// <returns>UTC string for SchedulesDirect</returns>
    private string ConvertDateTime(DateTime dateTime)
    {
      return dateTime.ToUniversalTime().ToString(@"yyyy-MM-dd\THH:mm:ss\Z", CultureInfo.InvariantCulture);
    }

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
        _proxy.Dispose();
      }
    }
    #endregion

    #region Internal Web Service Proxy Class
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Web.Services.WebServiceBindingAttribute(Name = "xtvdBinding", Namespace = "urn:TMSWebServices")]
    class SchedulesDirectWebServiceProxy : System.Web.Services.Protocols.SoapHttpClientProtocol
    {
      public SchedulesDirectWebServiceProxy(string username, string password)
      {
        Credentials         = new NetworkCredential(username, password, String.Empty);
        Url                 = "http://dd.schedulesdirect.org/schedulesdirect/tvlistings/xtvdService";
        //Url                 = "http://webservices.schedulesdirect.tmsdatadirect.com/schedulesdirect/tvlistings/xtvdService";
        //Url                 = "http://datadirect.webservices.zap2it.com/tvlistings/xtvdService";
        PreAuthenticate     = true;
        EnableDecompression = true; // Enables gzip and deflate compression (docs are wrong, this is NOT the default)

        //TODO: Possible fix for WebRequest Timeout error
        //Timeout = System.Threading.Timeout.Infinite;
      }

      [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:TMSWebServices:xtvdWebService#acknowledge",
                                                            RequestNamespace = "urn:TMSWebServices", ResponseNamespace = "")]
      public object acknowledge()
      {
        try
        {
          object[] response = this.Invoke("acknowledge", new object[] { });
          return response[0];
        }
        catch
        {
          return null;
        }
      }

      [System.Web.Services.Protocols.SoapRpcMethodAttribute("urn:TMSWebServices:xtvdWebService#download",
                                                            RequestNamespace = "urn:TMSWebServices", ResponseNamespace = "")]
      public object download(string startTime, string endTime)
      {
        object[] response = this.Invoke("download", new object[] {
                                          startTime,
                                          endTime });
        return response[0];
      }

      public IAsyncResult Begindownload(string startTime, string endTime, AsyncCallback callback, object asyncState)
      {
        return this.BeginInvoke("download", new object[] {
                                  startTime,
                                  endTime}, callback, asyncState);
      }

      public object Enddownload(IAsyncResult asyncResult)
      {
        object[] results = this.EndInvoke(asyncResult);
        return results[0];
      }
    }
    #endregion



  }
}
