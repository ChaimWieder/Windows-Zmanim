# Windows-Zmanim
Portable Windows app for Zmanim. 

Run **Zmanim.exe** on Windows 10 or 11. Close the older version first. No installer, account, or API key is needed. Uses the Windows .NET Framework 4.x runtime. Controls are in English; zman names remain in Hebrew.

- Pick your city, or type a five-digit US ZIP directly and click **Refresh** (or press Enter). Typing automatically selects US ZIP mode.
- Check **Pin next zman** to show a small always-on-top next-zman widget. Click the widget to open the full app. The full app is never always on top.
- Click **Compact** to return to the widget. Uncheck **Pin next zman**, or close the widget, to turn off the widget and return to the full app.
- **Today** returns to today; use the date picker for another date.
- **24h** switches the clock format.
- Location and preferences are saved under `%LOCALAPPDATA%\YiddishZmanim`.
- Successfully retrieved days are cached. If a request fails, only cached data for the same location and date is shown, marked Offline.
- The next-zman countdown uses the selected location's time zone. Missing solar events display a dash.

Data: [Hebcal Zmanim API](https://www.hebcal.com/home/1663/zmanim-halachic-times-api), licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Internet is needed for new dates/locations. Times use Hebcal's default sea-level calculations and the methods shown beside each zman. Confirm your community's practice before relying on a particular calculation. This version does not include candle-lighting or holiday schedules.

Source is included in Program.cs. Rebuild from a Developer Command Prompt or with Windows' Framework compiler:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:Zmanim.exe /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Program.cs
```

When Pin next zman is selected, closing the main window hides it and keeps the popup running. Click the popup to reopen the full app. To exit, uncheck Pin next zman and close the main window.

### Disclaimer

This code was written by AI. Feel free to review it, add or remove features/zmanim, fix AI mistakes, or suggest better code.

Feel free to open an issue for any questions, or a pull request with your edits.
