using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;              //AVI
using System.Threading;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Management;


namespace Target_shooter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            BB_Target targetdata = new BB_Target();
            thread_com tc = new thread_com();
            
            serialcomm sc = new serialcomm(tc, targetdata);
            sc.init();

            Thread serialListnerThread = new Thread(new ThreadStart(sc.listner_thread));
            serialListnerThread.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(tc, targetdata));

            serialListnerThread.Abort();
        }
    }

    public class thread_com
    {
        Boolean UpdateNeeded;
        string serialportname;


        public event EventHandler RedrawForm;

        // An event that clients can use to be notified whenever the
        // elements of the list change:
        public event EventHandler Changed;

        // Invoke the Changed event; called whenever list changes:
        protected virtual void OnChanged(EventArgs e)
        {
            if (Changed != null)
                Changed(this, e);
        }

        public thread_com()
        {
            UpdateNeeded = false;
            serialportname = System.IO.Ports.SerialPort.GetPortNames()[0]; // Get first serial port
        }
        public Boolean Update_needed
        {
            get { return UpdateNeeded; }
           // set { UpdateNeeded = value; OnChanged(EventArgs.Empty); }
            set { UpdateNeeded = value; OnChanged(EventArgs.Empty); RedrawForm(this, EventArgs.Empty); }
        }
        public string Serial_port_name
        {
            get { return serialportname; }
            set { serialportname = value; OnChanged(EventArgs.Empty); }
        }
    }

    public class Piezo_Mic : IComparable<Piezo_Mic>
    {
        private string name;
        private long posX;
        private long posY;
        private long triggerTime;

        public int CompareTo(Piezo_Mic other) // to enable sort of class objects
        {
            // If other is not a valid object reference, this instance is greater.
            if (other == null) return 1;

            // The comparison depends on the comparison of 
            // the underlying Double values.
            return triggerTime.CompareTo(other.triggerTime);
        }

        public Piezo_Mic(long X, long Y, long triggerTimes, string names)
        {
            posX = X;
            posY = Y;
            name = names;
            triggerTime = triggerTimes;
        }

        public string Name{
            get{ return name;}
            set{ name = value; }
        }
        public long PosX
        {
            get { return posX; }
            set { posX = value; }
        }
        public long PosY
        {
            get { return posY; }
            set { posY = value; }
        }
        public long TriggerTime
        {
            get { return triggerTime; }
            set { triggerTime = value; }
        }
    }

    public class BB_Target
    {
        List<Piezo_Mic> Mics;
        List<Piezo_Mic> Mic_properties;
        int x_offset;
        int y_offset;
        int scaler;
        
        const int soundspeed_in_steel = 5000; // m/s
        const int sampling_frequency = 50000000; // MHz
        const double speed_factor = 1/((1.0 / sampling_frequency) * soundspeed_in_steel * 1000); // counts per mm (ca 10)
        public const int Pixel_per_mm = 3;
        
        public BB_Target(){
            Mics = new List<Piezo_Mic>();

            Mic_properties = new List<Piezo_Mic>();
            Mic_properties.Add(new Piezo_Mic((int)(-speed_factor * 105), 0, 0, "Left"));
            Mic_properties.Add(new Piezo_Mic((int)(speed_factor * 105), 0, 0, "Right"));
            Mic_properties.Add(new Piezo_Mic(0, (int)(speed_factor * 105), 0, "Up"));
            Mic_properties.Add(new Piezo_Mic(0, (int)(-speed_factor * 105), 0, "Down"));

            X_offset = 1024/2;
            Y_offset = 1024/2;
            scaler = 1;
            }
                    public int X_offset
                    {
                        get { return x_offset; }
                        set { x_offset = value; }
                    }
                    public int Y_offset
                    {
                        get { return y_offset; }
                        set { y_offset = value; }
                    }
                    public int Scaler
                    {
                        get{ return scaler; }
                        set{ scaler = value; }
                    }
                    public void add(long Time, int count){
                        Piezo_Mic p = Mic_properties.ElementAt(count);
                        Mics.Add(new Piezo_Mic( p.PosX, p.PosY, Time/scaler, p.Name));
                        Mics.Sort();
                        Mics.Reverse();
                    }
                    public void clear()
                    {
                        Mics.Clear();
                    }
                    public int count()
                    {
                        return Mics.Count();
                    }
                    public Circle Circle1()
                    {
                        if (Mics.Count > 3)
                        {
                            return new Circle(Mics.ElementAt(0).PosX+x_offset, Mics.ElementAt(0).PosY+y_offset, Mics.ElementAt(0).TriggerTime - Mics.ElementAt(3).TriggerTime);
                        }
                        else
                        {
                            return new Circle(300, 150, (int)(40 * 2.5));
                        }
                             

                    }
                    public Circle Circle2()
                    { 
                        if (Mics.Count > 3)
                        {
                            return new Circle(Mics.ElementAt(1).PosX+x_offset, Mics.ElementAt(1).PosY+y_offset, Mics.ElementAt(1).TriggerTime - Mics.ElementAt(3).TriggerTime);
                        }
                        else{
                            return new Circle(100, 200, (int)(70*1));
                        }

                    }
                    public Circle Circle3()
                    {
                        if (Mics.Count > 3)
                        {
                            return new Circle(Mics.ElementAt(2).PosX+x_offset, Mics.ElementAt(2).PosY+y_offset, Mics.ElementAt(2).TriggerTime - Mics.ElementAt(3).TriggerTime);
                        }
                        else{
                            return new Circle(200, 300, (int)(60 * 1.5));
                        }
                    }
                    public Circle[] Largest_3_Circles()
                    {
                        return new Circle[]
                        {
                            this.Circle1(), this.Circle2(), this.Circle3()
                        };

                    }
                    public Circle YinvCircle(Circle c)
                    {
                        c.Center.Y = this.y_offset - (c.Center.Y - this.y_offset);
                        return c;
                    }
  
                    public PointF ResultCoordinates(){
                        Circle resultCircle = new Circle( ResultCircle() );
                        if (resultCircle != null)
                        {
                            PointF ResultPoint = new PointF(resultCircle.Center.X, resultCircle.Center.Y);


                            return ResultPoint;
                        }
                        else
                        {
                            return new PointF(0,0);
                        }
                    }

                    public Circle ResultCircle()
                    {
                        return new Circle(FindApollonianCircle(this.Circle1(), this.Circle2(), this.Circle3(), 1, 1, 1));
                    }

                    public Circle ResultCircleYinv()
                    {
                        Circle c = new Circle(FindApollonianCircle(this.Circle1(), this.Circle2(), this.Circle3(), 1, 1, 1));
                        c.Center.Y = this.y_offset-(c.Center.Y-this.y_offset);
                        return c;    
                    }

                    public Circle SmallResultCircleinv()
                    {
                        Circle c = new Circle(FindApollonianCircle(this.Circle1(), this.Circle2(), this.Circle3(), 1, 1, 1));
                        c.Radius = 5;
                        c.Center.Y = this.y_offset - (c.Center.Y - this.y_offset);
                        return c;
                    }

                    // Find a solution to Apollonius' problem.
                    // For discussion and method, see:
                    //    http://mathworld.wolfram.com/ApolloniusProblem.html
                    //    http://en.wikipedia.org/wiki/Problem_of_Apollonius#Algebraic_solutions
                    // For most of a Java code implementation, see:
                    //    http://www.diku.dk/hjemmesider/ansatte/rfonseca/implementations/apollonius.html    
                    public Circle FindApollonianCircle(Circle c1, Circle c2, Circle c3, int s1, int s2, int s3)
                    {
                        // Make sure c2 doesn't have the same X or Y coordinate as the others.
                        const float tiny = 0.0001f;
                        if ((Math.Abs(c2.Center.X - c1.Center.X) < tiny) ||
                            (Math.Abs(c2.Center.Y - c1.Center.Y) < tiny))
                        {
                            Circle temp_circle = c2;
                            c2 = c3;
                            c3 = temp_circle;
                            int temp_s = s2;
                            s2 = s3;
                            s3 = temp_s;
                        }
                        if ((Math.Abs(c2.Center.X - c3.Center.X) < tiny) ||
                            (Math.Abs(c2.Center.Y - c3.Center.Y) < tiny))
                        {
                            Circle temp_circle = c2;
                            c2 = c1;
                            c1 = temp_circle;
                            int temp_s = s2;
                            s2 = s1;
                            s1 = temp_s;
                        }
                        Debug.Assert(
                            (c2.Center.X != c1.Center.X) && (c2.Center.Y != c1.Center.Y) &&
                            (c2.Center.X != c3.Center.X) && (c2.Center.Y != c3.Center.Y),
                            "Cannot find points without matching coordinates.");

                        float x1 = c1.Center.X;
                        float y1 = c1.Center.Y;
                        float r1 = c1.Radius;
                        float x2 = c2.Center.X;
                        float y2 = c2.Center.Y;
                        float r2 = c2.Radius;
                        float x3 = c3.Center.X;
                        float y3 = c3.Center.Y;
                        float r3 = c3.Radius;

                        float v11 = 2 * x2 - 2 * x1;
                        float v12 = 2 * y2 - 2 * y1;
                        float v13 = x1 * x1 - x2 * x2 + y1 * y1 - y2 * y2 - r1 * r1 + r2 * r2;
                        float v14 = 2 * s2 * r2 - 2 * s1 * r1;

                        float v21 = 2 * x3 - 2 * x2;
                        float v22 = 2 * y3 - 2 * y2;
                        float v23 = x2 * x2 - x3 * x3 + y2 * y2 - y3 * y3 - r2 * r2 + r3 * r3;
                        float v24 = 2 * s3 * r3 - 2 * s2 * r2;

                        float w12 = v12 / v11;
                        float w13 = v13 / v11;
                        float w14 = v14 / v11;

                        float w22 = v22 / v21 - w12;
                        float w23 = v23 / v21 - w13;
                        float w24 = v24 / v21 - w14;

                        float P = -w23 / w22;
                        float Q = w24 / w22;
                        float M = -w12 * P - w13;
                        float N = w14 - w12 * Q;

                        float a = N * N + Q * Q - 1;
                        float b = 2 * M * N - 2 * N * x1 + 2 * P * Q - 2 * Q * y1 + 2 * s1 * r1;
                        float c = x1 * x1 + M * M - 2 * M * x1 + P * P + y1 * y1 - 2 * P * y1 - r1 * r1;

                        // Find roots of a quadratic equation
                        double[] solutions = QuadraticSolutions(a, b, c);
                        if (solutions.Length < 1) return null;
                        float rs = (float)solutions[0];
                        float xs = M + N * rs;
                        float ys = P + Q * rs;

                        return new Circle(xs, ys, rs);
                    }

                    // Return solutions to a quadratic equation.
                    private double[] QuadraticSolutions(double a, double b, double c)
                    {
                        const double tiny = 0.000001;
                        double discriminant = b * b - 4 * a * c;

                        // See if there are no real solutions.
                        if (discriminant < 0)
                        {
                            return new double[] { };
                        }

                        // See if there is one solution.
                        if (discriminant < tiny)
                        {
                            return new double[] { -b / (2 * a) };
                        }

                        // There are two solutions.
                        return new double[]
                        {
                            (-b + Math.Sqrt(discriminant)) / (2 * a),
                            (-b - Math.Sqrt(discriminant)) / (2 * a),
                        };
                    }
                }

                class serialcomm{
                    static SerialPort _serialPort;
                    StringBuilder incomming = new StringBuilder();
                    BB_Target targetdata;
                    thread_com tc;

                    public const char CR = (char)13;
                    public const char LF = (char)10;

                    public serialcomm(thread_com t, BB_Target b){
                        targetdata=b;
                        tc = t;

                        tc.Changed += new EventHandler(PortChanged);
                    }

                    private void PortChanged(object sender, EventArgs e)
                    {
                        if (tc.Serial_port_name != _serialPort.PortName)
                        {
                            _serialPort.Close();
                            this.init();
                        }
                    }

                    public bool init(){
                        _serialPort = new SerialPort();         // Create a new SerialPort object with default settings
            
                        _serialPort.PortName = tc.Serial_port_name;
                        _serialPort.BaudRate = 9600;
                        _serialPort.DataBits = 8;
                        _serialPort.Parity = Parity.None;
                        _serialPort.StopBits = StopBits.One;
                        _serialPort.Handshake = Handshake.None;

                        try
                        {
                            _serialPort.Open();
                        }
                        catch
                        {
                            // error handeling
                            return false;
                        }
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.WriteLine("Application started! \n" + CR);
                        }
                        return true;
                    }

                    public void listner_thread(){ // Listens on serial port, decodes and send commands on queue
        
                  /*Start av spel:  SG:5<CR>                           StartGame : Hex number of bullets <CR>
                    Statistik:      ST<CR>                             STatistics <CR>
                    Print:          PR<CR>                             PRint <CR>
                    Fel:            ER:Något fel\nsom är trasigt.<CR>  ERror : Text to display <CR>
                    Skottdata:      SD:FFFFF:FFFFF:FFFFF:FFFFF:1<CR>   ShootData : Hex left : Hex right : Hex up : hex down : Hex bullet number <CR>
                    Avsluta spel:   EG<CR>                             EndGame <CR>
                    Skalfaktor:     SC:5000<CR>                        Decimal Konstant som skall divideras med inkommande Hex värden för korrekt skalning till display på tavlan

                    <LF> skall ignoreras.
                    Checksummor?
                    Ack - Nack?
               */         
            while (true)
            {
                while (_serialPort.IsOpen == false) ;

                char ch='\0';
                char[] delimiterChars = { ':', ':', ':', ':', ':' }; // for splitting the SD incomming command

                try
                {
                    ch = (char)_serialPort.ReadChar();
                    if (ch != LF) 
                    { 
                        _serialPort.Write(ch.ToString());
                        incomming.Append(ch.ToString());
                    }
                }
                catch
                {

                }

                
                if (ch == CR && incomming.Length >=2 ) // om enter teckan kontrollera buffern.
                {
                    _serialPort.WriteLine("" + CR);
                    switch ((incomming.ToString()).Substring(0,2))
                    {
                        case "SD":  string[] words = incomming.ToString().Split(delimiterChars);
                                    int i = 0;

                                    targetdata.clear();
                                    foreach(string Str in words){
                                        if (this.OnlyHexInString(Str) && i<4)
                                        {
                                            targetdata.add(long.Parse(Str, System.Globalization.NumberStyles.HexNumber), i);
                                            i++;
                                        }
                                    }
                                    if (i == 4) // alla 4st tal emottagna
                                    {
                                        _serialPort.WriteLine("Hitpoint X=" + (targetdata.ResultCoordinates().X-targetdata.X_offset) + " Hitpoint Y=" + (targetdata.ResultCoordinates().Y-targetdata.Y_offset) + CR);
                                        // Rita grafik
                                    }

                                    break;
                        case "SG": int bullits;
                                   string[] numbers = Regex.Split(incomming.ToString(), @"\D+");
                                   bullits = int.Parse(numbers[1]);
                                   _serialPort.WriteLine("OK: Start game with " + bullits + " bullits" + CR);
                                    break;
                        case "ST": _serialPort.WriteLine("OK: Displaying stat page" + CR);
                                    break;
                        case "PR": _serialPort.WriteLine("OK: Printing current Game" + CR);
                                    break;
                        case "EG": _serialPort.WriteLine("OK: Current game ended" + CR);
                                    break;
                        case "SC": string[] num = Regex.Split(incomming.ToString(), @"\D+");
                                   targetdata.Scaler = int.Parse(num[1]);
                                   _serialPort.WriteLine("OK: divider/scaler set to: " + targetdata.Scaler + CR);
                                    break;
                        case "OX": string[] numX = Regex.Split(incomming.ToString(), @"\D+");
                                    targetdata.X_offset = int.Parse(numX[1]);
                                    _serialPort.WriteLine("OK: X offset set to: " + targetdata.X_offset + CR);
                                    break;
                        case "OY": string[] numY = Regex.Split(incomming.ToString(), @"\D+");
                                    targetdata.Y_offset = int.Parse(numY[1]);
                                    _serialPort.WriteLine("OK: Y offset set to: " + targetdata.Y_offset + CR);
                                    break;
                        case "ER":
                        default: _serialPort.WriteLine("Error!" + CR);
                                    break;
                    }
                    tc.Update_needed = true;
                    incomming.Length = 0; //Töm inbuffern
                }
                else if (ch == CR)
                {
                    _serialPort.WriteLine("Error!" + CR);
                    incomming.Length = 0; //Töm inbuffern
                }
            }
        }

        public bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }
    }
}
