using System;
using System.Xml;
using System.Ext.Xml;
using System.IO;
using Microsoft.SPOT;

namespace M8_Brewery
{
    class M8_DataLog
    {

        MemoryStream _stream;
        XmlWriter _writer;

        public M8_DataLog()
        {
            _stream = new MemoryStream();

            startLog();
        }

        public void addTimeStamp()
        {
            _writer.WriteStartElement("Time");

            _writer.WriteStartElement("DateTime");
            _writer.WriteString(DateTime.Now.ToString());
            _writer.WriteEndElement();

            _writer.WriteEndElement();
        }
        public void addSSRData(int watching, float value)
        {
            _writer.WriteStartElement("SSR"); //</sample>            

            _writer.WriteStartElement("Sensor");//child element
            _writer.WriteString(watching.ToString());
            _writer.WriteEndElement();

            _writer.WriteStartElement("Power");
            _writer.WriteString(value.ToString());
            _writer.WriteEndElement();

            _writer.WriteEndElement(); //</sample>

            _writer.WriteRaw("\r\n");
        }
        public void addPIDData(int watching, float value)
        {
            _writer.WriteStartElement("PID"); //</sample>            

            _writer.WriteStartElement("Sensor");//child element
            _writer.WriteString(watching.ToString());
            _writer.WriteEndElement();

            _writer.WriteStartElement("Value");
            _writer.WriteString(value.ToString());
            _writer.WriteEndElement();

            _writer.WriteEndElement(); //</sample>

            _writer.WriteRaw("\r\n");
        }
        public void addTempData(int sensor, float temp, float target)
        {
            _writer.WriteStartElement("Temp"); //</sample>            

                _writer.WriteStartElement("Sensor");//child element
                _writer.WriteString(sensor.ToString());
                _writer.WriteEndElement();

                _writer.WriteStartElement("Current");
                _writer.WriteString(temp.ToString());
                _writer.WriteEndElement();

                _writer.WriteStartElement("Target");
                _writer.WriteString(target.ToString());
                _writer.WriteEndElement();

            _writer.WriteEndElement(); //</sample>

            _writer.WriteRaw("\r\n");
        }

        public void startLog()
        {
            _writer = XmlWriter.Create(_stream);

            _writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");

            _writer.WriteStartElement("M8_Data");//root element
//            _writer.WriteComment( DateTime.Now.ToString() );
        }

        public void stopLog()
        {
            _writer.WriteEndElement();//end the root element

            _writer.Flush();
            _writer.Close();
        }

        public string dumpData()
        {

            stopLog();

            //////// display the XML data ///////////
            byte[] byteArray = _stream.ToArray();
            return (new string(System.Text.UTF8Encoding.UTF8.GetChars(byteArray)));
        }
        
    }
}
