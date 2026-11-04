using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

class ZmanimApp : Form {
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
    static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YiddishZmanim");
    readonly ComboBox city = new ComboBox();
    readonly TextBox zip = new TextBox();
    readonly DateTimePicker date = new DateTimePicker();
    readonly CheckBox pin = new CheckBox(), clock24 = new CheckBox(), countdown = new CheckBox();
    readonly bool selfTesting;
    readonly Label status = new Label(), next = new Label(), heading = new Label();
    readonly DataGridView grid = new DataGridView();
    readonly Button refresh = new Button();
    readonly Form widget = new Form();
    readonly Label widgetText = new Label();
    bool shuttingDown;
    readonly Timer timer = new Timer { Interval = 1000 };
    readonly List<Tuple<string,string>> fields = new List<Tuple<string,string>>();
    Dictionary<string,object> times;
    TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    bool busy, automaticDate = true;
    string loadedKey = "";
    readonly string[] places = {"Brooklyn", "Monsey", "Kiryas Joel", "Lakewood", "Manchester", "London", "Antwerp", "Jerusalem", "Bnei Brak", "US ZIP code"};
    readonly string[] locations = {"geonameid=5110302", "geonameid=5127315", "geonameid=5123344", "geonameid=5100280", "geonameid=2643123", "geonameid=2643743", "geonameid=2803138", "geonameid=281184", "geonameid=295514"};
    readonly string[] zones = {"Eastern Standard Time","Eastern Standard Time","Eastern Standard Time","Eastern Standard Time","GMT Standard Time","GMT Standard Time","Romance Standard Time","Israel Standard Time","Israel Standard Time"};
    [STAThread] static void Main(string[] args) {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Contains("--self-test")) { try { SelfTest(); } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-result.txt"), ex.ToString()); } return; }
        Application.Run(new ZmanimApp());
    }
    public ZmanimApp(bool testing=false) {
        selfTesting=testing;
        Text = "Yiddish Zmanim"; Size = new Size(520,800); MinimumSize = new Size(520,640);
        BackColor = Color.FromArgb(242,245,249); ForeColor = Color.FromArgb(25,42,62); Font = new Font("Segoe UI",10);
        StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=7, Padding=new Padding(20) };
        foreach(int h in new[]{65,44,72,40,96}) root.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute,56)); Controls.Add(root);
        heading.Text="Yiddish Zmanim"; heading.Font=new Font("Segoe UI",25,FontStyle.Bold); heading.Dock=DockStyle.Fill; heading.TextAlign=ContentAlignment.MiddleLeft; root.Controls.Add(heading);
        city.DropDownStyle=ComboBoxStyle.DropDownList; city.Items.AddRange(places); city.SelectedIndex=0; city.Dock=DockStyle.Fill; root.Controls.Add(city);
        var options=new FlowLayoutPanel { Dock=DockStyle.Fill }; zip.Width=75; zip.MaxLength=5; zip.Text="11211"; zip.Enabled=true;
        options.Controls.Add(new Label { Text="US ZIP", AutoSize=true, Padding=new Padding(0,5,0,0) }); options.Controls.Add(zip);
        pin.Text="Pin next zman"; pin.AutoSize=true; clock24.Text="24h"; clock24.AutoSize=true; options.Controls.Add(pin); options.Controls.Add(clock24);
        var compact=new Button { Text="Compact", Width=80 }; options.Controls.Add(compact); root.Controls.Add(options);
        countdown.Text="Show countdown"; countdown.AutoSize=true; options.SetFlowBreak(compact,true); options.Controls.Add(countdown);
        var dates=new FlowLayoutPanel { Dock=DockStyle.Fill }; date.Format=DateTimePickerFormat.Custom; date.CustomFormat="ddd, MMM d, yyyy"; date.Width=180;
        var today=new Button { Text="Today", Width=65 }; refresh.Text="Refresh"; refresh.Width=100; dates.Controls.Add(date); dates.Controls.Add(today); dates.Controls.Add(refresh); root.Controls.Add(dates);
        next.Dock=DockStyle.Fill; next.BackColor=Color.FromArgb(24,57,78); next.ForeColor=Color.White; next.Font=new Font("Segoe UI",12,FontStyle.Bold); next.TextAlign=ContentAlignment.MiddleCenter; next.Text="Loading zmanim…"; root.Controls.Add(next);
        grid.Dock=DockStyle.Fill; grid.BackgroundColor=Color.White; grid.BorderStyle=BorderStyle.None; grid.ReadOnly=true; grid.AllowUserToAddRows=false; grid.AllowUserToDeleteRows=false; grid.AllowUserToResizeRows=false; grid.RowHeadersVisible=false; grid.ColumnHeadersVisible=false; grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect; grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal; grid.GridColor=Color.FromArgb(235,239,243); grid.RowTemplate.Height=38;
        grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(223,236,242); grid.DefaultCellStyle.SelectionForeColor=ForeColor; grid.DefaultCellStyle.Padding=new Padding(8,0,8,0); grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(247,249,251);
        grid.Columns.Add("time","Time"); grid.Columns.Add("name","Zman"); grid.Columns[0].FillWeight=38; grid.Columns[1].FillWeight=62; grid.Columns[1].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight; root.Controls.Add(grid);
        status.Dock=DockStyle.Fill; status.TextAlign=ContentAlignment.MiddleCenter; status.Font=new Font("Segoe UI",8); root.Controls.Add(status);
        Add("alotHaShachar","עלות השחר · 16.1°"); Add("misheyakir","טלית ותפילין · 11.5°"); Add("sunrise","הנץ החמה"); Add("sofZmanShmaMGA","סוף זמן ק״ש · מג״א"); Add("sofZmanShma","סוף זמן ק״ש · גר״א"); Add("sofZmanTfillaMGA","סוף זמן תפילה · מג״א"); Add("sofZmanTfilla","סוף זמן תפילה · גר״א"); Add("chatzot","חצות היום"); Add("minchaGedola","מנחה גדולה"); Add("minchaKetana","מנחה קטנה"); Add("plagHaMincha","פלג המנחה"); Add("sunset","שקיעה"); Add("tzeit72min","רבינו תם · 72 min");
        widget.Text="Next zman · Click to open"; widget.Size=new Size(370,160); widget.MinimumSize=new Size(300,150); widget.FormBorderStyle=FormBorderStyle.SizableToolWindow; widget.StartPosition=FormStartPosition.Manual; widget.Location=new Point(Screen.PrimaryScreen.WorkingArea.Right-390,Screen.PrimaryScreen.WorkingArea.Top+40); widget.BackColor=next.BackColor;
        widgetText.Dock=DockStyle.Fill; widgetText.ForeColor=Color.White; widgetText.Font=new Font("Segoe UI",12,FontStyle.Bold); widgetText.TextAlign=ContentAlignment.MiddleCenter; widgetText.Cursor=Cursors.Hand; widget.Controls.Add(widgetText);
        next.TextChanged+=(s,e)=>widgetText.Text=next.Text; widgetText.Text=next.Text;
        widgetText.Click+=(s,e)=>{Show();WindowState=FormWindowState.Normal;Activate();};
        widget.FormClosing+=(s,e)=>{if(!shuttingDown){e.Cancel=true;pin.Checked=false;widget.Hide();Show();Activate();}};
        LoadSettings(); TopMost=false;
        countdown.CheckedChanged+=(s,e)=>{UpdateNext();SaveSettings();};
        pin.CheckedChanged+=(s,e)=>{TopMost=false;widget.TopMost=pin.Checked;if(pin.Checked){widget.Show();Hide();}else{widget.Hide();Show();} SaveSettings();}; clock24.CheckedChanged+=(s,e)=>{Render();SaveSettings();};
        compact.Click+=(s,e)=>{if(!pin.Checked)pin.Checked=true;else {widget.Show();Hide();} automaticDate=true; date.Value=TimeZoneInfo.ConvertTime(DateTimeOffset.Now,zone).Date;automaticDate=true;};
        city.SelectionChangeCommitted+=(s,e)=>{BeginInvoke(new Action(async()=>{times=null;SaveSettings();await Fetch();}));};
        city.DropDown+=(s,e)=>{timer.Stop();root.SuspendLayout();};
        city.DropDownClosed+=(s,e)=>{root.ResumeLayout();timer.Start();};
        zip.TextChanged+=(s,e)=>{city.SelectedIndex=9;};
        date.ValueChanged+=async(s,e)=>{automaticDate=false; await Fetch();}; refresh.Click+=async(s,e)=>await Fetch();
        today.Click+=async(s,e)=>{automaticDate=true; date.Value=TimeZoneInfo.ConvertTime(DateTimeOffset.Now,zone).Date; automaticDate=true; await Fetch();};
        zip.KeyDown+=async(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Fetch();}};
        timer.Tick+=async(s,e)=>{if(city.DroppedDown)return;UpdateNext(); var d=TimeZoneInfo.ConvertTime(DateTimeOffset.Now,zone).Date; if(automaticDate && date.Value.Date!=d && !busy){date.Value=d;automaticDate=true;await Fetch();}};
        FormClosing+=(s,e)=>{
            SaveSettings();
            if(e.CloseReason==CloseReason.UserClosing && pin.Checked && !shuttingDown){
                e.Cancel=true; widget.TopMost=true; widget.Show(); Hide(); return;
            }
            shuttingDown=true;widget.Close();
        }; Shown+=async(s,e)=>{timer.Start();if(pin.Checked){widget.TopMost=true;widget.Show();Hide();}if(!selfTesting)await Fetch();};
    }
    void Add(string key,string title){fields.Add(Tuple.Create(key,title));}
    string Key(){return (city.SelectedIndex==9?"zip="+Uri.EscapeDataString(zip.Text.Trim()):locations[city.SelectedIndex])+"&date="+date.Value.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
    async Task Fetch(){
        if(busy) return;
        if(city.SelectedIndex==9 && (zip.Text.Length!=5 || !zip.Text.All(char.IsDigit))){status.Text="Enter a valid five-digit US ZIP code, then click Refresh.";return;}
        busy=true; refresh.Enabled=false; status.Text="Loading from Hebcal…"; times=null; grid.Rows.Clear(); next.Text="Loading zmanim…";
        string key=Key(); string cache=Path.Combine(Folder,"cache-"+key.Replace('=','_').Replace('&','_')+".json"); bool cached=false, failed=false;
        try {
            string raw;
            try { raw=await Http.GetStringAsync("https://www.hebcal.com/zmanim?cfg=json&"+key); Parse(raw); Directory.CreateDirectory(Folder); try{File.WriteAllText(cache,raw);}catch(IOException){} }
            catch(Exception) { if(!File.Exists(cache))throw; raw=File.ReadAllText(cache); Parse(raw); cached=true; }
            await WaitForSelection();
            loadedKey=key; Render(); status.Text=(cached?"Offline · Cached times":"Updated")+" · "+zone.DisplayName+"\nHebcal.com • CC BY 4.0 • Methods shown beside each zman"; SaveSettings();
        } catch(Exception){failed=true;}
        finally {busy=false;}
        await WaitForSelection();if(IsDisposed)return;refresh.Enabled=true;
        if(failed){await WaitForSelection();if(IsDisposed)return;times=null;grid.Rows.Clear();next.Text="Zmanim unavailable";status.Text="Connection failed or invalid location. Please try again.";}
        if(Key()!=key) await Fetch();
    }
    void Parse(string raw){var data=Json.Deserialize<Dictionary<string,object>>(raw); var loc=(Dictionary<string,object>)data["location"]; string iana=(string)loc["tzid"]; string windows;
        var map=new Dictionary<string,string>{{"America/New_York","Eastern Standard Time"},{"America/Chicago","Central Standard Time"},{"America/Denver","Mountain Standard Time"},{"America/Phoenix","US Mountain Standard Time"},{"America/Los_Angeles","Pacific Standard Time"},{"America/Anchorage","Alaskan Standard Time"},{"Pacific/Honolulu","Hawaiian Standard Time"},{"Europe/London","GMT Standard Time"},{"Europe/Brussels","Romance Standard Time"},{"Asia/Jerusalem","Israel Standard Time"}};
        if(!map.TryGetValue(iana,out windows))throw new InvalidDataException("Unsupported timezone"); zone=TimeZoneInfo.FindSystemTimeZoneById(windows); times=(Dictionary<string,object>)data["times"];
    }
    DateTimeOffset? Value(string k){object v;DateTimeOffset dt;return times!=null && times.TryGetValue(k,out v) && v!=null && DateTimeOffset.TryParse(v.ToString(),CultureInfo.InvariantCulture,DateTimeStyles.None,out dt)?dt:(DateTimeOffset?)null;}
    async Task WaitForSelection(){while(!IsDisposed && city.DroppedDown)await Task.Delay(100);}
    void Render(){if(times==null || city.DroppedDown)return;grid.Rows.Clear();foreach(var f in fields){var t=Value(f.Item1);grid.Rows.Add(t.HasValue?t.Value.ToString(clock24.Checked?"HH:mm":"h:mm tt",CultureInfo.InvariantCulture):"—",f.Item2);} UpdateNext();}
    void UpdateNext(){UpdateNext(DateTimeOffset.Now);}
    void UpdateNext(DateTimeOffset now){if(times==null || city.DroppedDown)return;var upcoming=fields.Select(f=>new{F=f,T=Value(f.Item1)}).Where(x=>x.T.HasValue && x.T.Value>now).OrderBy(x=>x.T.Value).FirstOrDefault();
        if(date.Value.Date!=TimeZoneInfo.ConvertTime(now,zone).Date){next.Text="Zmanim for "+date.Value.ToString("MMM d, yyyy");return;}
        if(upcoming==null){next.Text="No more zmanim today";return;}
        string text="Next: "+upcoming.F.Item2+"\n"+upcoming.T.Value.ToString(clock24.Checked?"HH:mm":"h:mm tt",CultureInfo.InvariantCulture);
        if(countdown.Checked){var left=upcoming.T.Value-now;text+="\nIn "+((int)left.TotalHours).ToString("00")+":"+left.Minutes.ToString("00")+":"+left.Seconds.ToString("00");}
        next.Text=text;
    }
    void LoadSettings(){if(selfTesting)return;try{var s=Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(Folder,"settings.json")));city.SelectedIndex=Math.Max(0,Math.Min(9,Convert.ToInt32(s["city"])));zip.Text=(string)s["zip"];pin.Checked=(bool)s["pin"];clock24.Checked=(bool)s["clock24"];countdown.Checked=s.ContainsKey("countdown") && (bool)s["countdown"];TopMost=false;zip.Enabled=true;}catch{} }
    void SaveSettings(){if(selfTesting)return;try{Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"settings.json"),Json.Serialize(new{city=city.SelectedIndex,zip=zip.Text,pin=pin.Checked,clock24=clock24.Checked,countdown=countdown.Checked}));}catch{}}
    static void SelfTest(){
        var raw=Json.Serialize(new { location=new {tzid="America/New_York"}, times=new {sunrise="2026-11-02T06:28:00-05:00",sunset="2026-11-02T16:50:00-05:00"} });
        using(var app=new ZmanimApp(true)){
            app.Parse(raw); app.Render();
            if(app.grid.Rows.Count!=14)throw new Exception("Missing rows");
            app.busy=true;app.date.Value=new DateTime(2026,11,2);app.busy=false;
            var now=new DateTimeOffset(2026,11,2,6,0,0,TimeSpan.FromHours(-5));
            app.UpdateNext(now);
            if(app.countdown.Checked || !app.widgetText.Text.Contains("6:28 AM") || app.widgetText.Text.Contains("\nIn "))throw new Exception("Default must show clock time only");
            app.countdown.Checked=true;app.UpdateNext(now);
            if(!app.widgetText.Text.Contains("6:28 AM\nIn 00:28:00"))throw new Exception("Optional countdown missing");
            app.clock24.Checked=true;app.UpdateNext(now);
            if(!app.widgetText.Text.Contains("06:28\nIn 00:28:00"))throw new Exception("24h clock time missing");
            app.countdown.Checked=false;app.UpdateNext(now);
            if(app.widgetText.Text.Contains("\nIn "))throw new Exception("Countdown did not turn off");
            app.pin.Checked=false; app.pin.Checked=true;
            if(app.TopMost || !app.widget.TopMost)throw new Exception("Widget TopMost failed");
            app.Show();app.Close();
            if(app.IsDisposed || app.Visible || !app.widget.Visible || !app.widget.TopMost)throw new Exception("Closing pinned main window must preserve popup");
            app.pin.Checked=false;
            if(!app.Visible || app.TopMost)throw new Exception("Full window failed");
            if(!app.zip.Enabled)throw new Exception("ZIP disabled");
            app.zip.Text="10001"; if(app.city.SelectedIndex!=9)throw new Exception("ZIP mode failed");
            app.next.Text="Selection in progress"; app.city.DroppedDown=true; if(app.timer.Enabled)throw new Exception("Timer must pause while selecting"); app.UpdateNext(); if(app.next.Text!="Selection in progress")throw new Exception("Countdown changed during selection"); var until=DateTime.UtcNow.AddSeconds(3); while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(20);} if(!app.city.DroppedDown)throw new Exception("Dropdown closed during three-second selection"); app.city.DroppedDown=false; if(!app.timer.Enabled)throw new Exception("Timer did not resume");
            app.clock24.Checked=true;app.Render();
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-result.txt"),"PASS: closing pinned main window preserves popup; fixture parsing, rows, widget-only TopMost, full window, editable ZIP, automatic ZIP mode, dropdown remains open during countdown update.");
            app.shuttingDown=true;app.widget.Close();
        }
    }
}
