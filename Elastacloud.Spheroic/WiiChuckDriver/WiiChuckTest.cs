namespace NetduinoSerbRemote
{
    public class WiiChuckTest
    {
        private WiiChuck wiiChuck;

        public WiiChuckTest()
        {
            wiiChuck = new WiiChuck(true);
        }

        public void Run()
        {
            while (true)
            {
                // try to read the data from nunchucku
                if (wiiChuck.GetData() == false)
                {
                    WiiChuck.PrintData(wiiChuck);
                }
            }
        }
    }
}