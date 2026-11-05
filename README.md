# Windows-Zmanim
Portable Windows app for Zmanim. 

Run **Zmanim.exe** on Windows 10 or 11. No installer, account, or API key is needed. Uses the Windows .NET Framework 4.x runtime. Controls are in English; zman names are in Hebrew.

- Pick your city, or type a five-digit US ZIP directly and click **Refresh** (or press Enter). Typing automatically selects US ZIP mode.
- Check **Pin next zman** to show a small always-on-top next-zman widget. Click the widget to open the full app. The full app is never always on top.
- Click **Compact** to return to the widget. Uncheck **Pin next zman**, or close the widget, to turn off the widget and return to the full app.
- **Today** returns to today; use the date picker for another date.
- **24h** switches the clock format.
- Location and preferences are saved under `%LOCALAPPDATA%\Zmanim`.
- Successfully retrieved days are cached. If a request fails, only cached data for the same location and date is shown, marked Offline.
- The next-zman countdown uses the selected location's time zone. Missing solar events display a dash.

Data: [Hebcal Zmanim API](https://www.hebcal.com/home/1663/zmanim-halachic-times-api), licensed [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Internet is needed for new dates/locations. Times use Hebcal's default sea-level calculations and the methods shown beside each zman. Confirm your community's practice before relying on a particular calculation. This version does not include candle-lighting or holiday schedules.

Source is included in Program.cs. Rebuild from a Developer Command Prompt or with Windows' Framework compiler:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:Zmanim.exe /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll Program.cs
```

When Pin next zman is selected, closing the main window hides it and keeps the popup running. Click the popup to reopen the full app. To exit, uncheck Pin next zman and close the main window.

Choose zmanim: use the Choose zmanim menu in the main window to check or uncheck individual zmanim. Your choices apply to both the daily list and next-zman widget and are saved between launches. Tzeit 8.5 degrees is off by default and can be enabled from this menu. If all are unchecked, the widget asks you to choose zmanim.

The app loads the selected day and the following day together. After the final selected zman of today, the widget shows the next selected zman tomorrow with a Tomorrow label. The optional countdown crosses midnight correctly. The list continues to show the day selected in the date picker; in automatic Today mode, it advances at midnight. New two-day results need an internet connection once before they are cached.

Candle lighting is taken directly from https://www.hebcal.com/home/195/jewish-calendar-rest-api using the selected location and Hebcal's local-custom defaults, with Israel holiday observance for Jerusalem/Bnei Brak and Diaspora observance elsewhere. No fixed offset is subtracted by the app: holiday times after nightfall are retained as supplied by Hebcal. New candle results need internet access; cached candle results are used on failure. A separate status message reports if candle times cannot be loaded, while ordinary zmanim remain available.

### Disclaimer

This code was written by AI. Feel free to review it, add or remove features/zmanim, fix AI mistakes, or suggest better code.

Feel free to open an issue for any questions, or a pull request with your edits.

We did edit and fix some of it, but there is always place for improvement.
