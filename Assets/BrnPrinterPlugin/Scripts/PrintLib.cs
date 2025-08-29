using UnityEngine; 
using System;
using System.Runtime.InteropServices; 

namespace PrintLib
{
    // text alignment & style
    public enum TextHorizAlignment { Left = 0, Center = 1, Right = 2 }
    public enum TextVertAlignment { Top = 0, Center = 1, Bottom = 2 }
    public enum TextFontStyle { Regular = 0, Bold = 1, Italic = 2, BoldItalic = 3, Underline = 4, Strikeout = 8 }
    // printer settings
    public enum Orientation { Default = 0, Portrait = 1, Landscape = 2 }
    public enum PaperFormat { Default = 0, A4 = 9, A2 = 66, A3 = 8, A5 = 11, Letter = 1, LetterSmall = 2 }
    public enum ColorMode { Default = 0, Monochrome = 1, Color = 2 }
    public enum DuplexMode {
        Default = 0,
        /// <summary>Normal (nonduplex) printing</summary>
        NoDuplex = 1,
        /// <summary>Short-edge binding, that is, the long edge of the page is horizontal.</summary>
        Horizontal = 2,
        /// <summary>Long-edge binding, that is, the long edge of the page is vertical.</summary>
        Vertical = 3
    }

    public class Printer
    {
        public const int OK = 0;
        public const int NOT_OK = -1;

        public int initResult = -1;

        // enable debug mode to have more detailed info in the console
        public bool showDebug = false;

        [DllImport("PrintLibDll")]
        private static extern int Init();

        [DllImport("PrintLibDll")]
        private static extern void Close();

        [DllImport("PrintLibDll")]
        private static extern int _GetPrinterCount();

        [DllImport("PrintLibDll")]
        private static extern IntPtr _GetPrinterName(int index);

        [DllImport("PrintLibDll")]
        private static extern IntPtr _GetDefaultPrinterName();

        [DllImport("PrintLibDll")]
        private static extern void _SelectPrinter(IntPtr printerName);

        [DllImport("PrintLibDll")]
        private static extern int _SetPrinterSettings(int orientation, int paperSize, float customWidth, float customHeight, int dpi, int colorMode, int copies, int duplex);

        [DllImport("PrintLibDll")]
        private static extern int _ShowPrinterDialog();

        [DllImport("PrintLibDll")]
        private static extern void _ResetPrinterSettings();

        [DllImport("PrintLibDll")]
        private static extern int _GetPageSize(ref float width, ref float height);

        [DllImport("PrintLibDll")]
        private static extern int _GetLastGdiStatus();

        [DllImport("PrintLibDll")]
        private static extern int _StartDocument();

        [DllImport("PrintLibDll")]
        private static extern void _SetPrintPosition(float left_mm, float top_mm);

        [DllImport("PrintLibDll")]
        private static extern int _NewPage();

        [DllImport("PrintLibDll")]
        private static extern int _EndDocument();

        [DllImport("PrintLibDll")]
        private static extern int _PrintImageFromStream(IntPtr lpData, uint dwCount, float width_mm, float height_mm);

        [DllImport("PrintLibDll")]
        private static extern int _PrintImageFromFile(IntPtr path, float width_mm, float height_mm);

        [DllImport("PrintLibDll")]
        private static extern int _PrintText(IntPtr text, float width_mm = 0, float height_mm = 0, int alignment = 0, int lineAlignment = 0);

        [DllImport("PrintLibDll")]
        private static extern void _SetTextColor(int R, int G, int B);
        
        [DllImport("PrintLibDll")]
        private static extern void _SetTextFontSize(float size);

        [DllImport("PrintLibDll")]
        private static extern void _SetTextFontFamily(IntPtr name);

        [DllImport("PrintLibDll")]
        private static extern void _SetTextFontStyle(int style);

        public Printer()
        {
            initResult = Init();
            if (initResult == NOT_OK) Debug.LogError("Printer Plugin, errors initializing printer library" + CheckGdiError());
		}

        ~Printer()
        {
            Close();
        }

        public int GetPrinterCount()
        {
            return _GetPrinterCount();
        }

        public string GetPrinterName(int index)
        {
            return Marshal.PtrToStringUni(_GetPrinterName(index));
        }

        public string GetDefaultPrinterName()
        {
            return Marshal.PtrToStringUni(_GetDefaultPrinterName());
        }

        public void SelectPrinter(string printerName)
        {
            if (showDebug) Debug.Log("Selecting printer " + printerName);
            bool found = false;
            int printerCount = GetPrinterCount();
            for (int i = 0; i < printerCount; i++)
            {
                if (GetPrinterName(i).ToLower() == printerName.ToLower())
                {
                    found = true;
                    break;
                }
            }
            if (!found) Debug.Log("Warning: selected printer \"" + printerName + "\" not recognized");
            _SelectPrinter(Marshal.StringToHGlobalUni(printerName));
        }

        public int SetPrinterSettings(Orientation orientation, PaperFormat paperFormat, int dpi = 0, ColorMode colorMode = ColorMode.Default, int copies = 0, DuplexMode duplex = DuplexMode.Default)
        {
			// note: a value of 0 means that the setting won't be changed and the default value will be used instead
			int res = _SetPrinterSettings((int)orientation, (int)paperFormat, 0, 0, dpi, (int)colorMode, copies, (int)duplex);
			if (res == NOT_OK) Debug.Log("Printer Plugin > SetPrinterSettings error" + CheckGdiError());
            return res;
        }

        public int SetPrinterSettings(Orientation orientation, float customPaperWidth, float customPaperHeight, int dpi = 0, ColorMode colorMode = ColorMode.Default, int copies = 0, DuplexMode duplex = DuplexMode.Default)
        {
            // note: a value of 0 means that the setting won't be changed and the default value will be used instead
            int res = _SetPrinterSettings((int)orientation, 256, customPaperWidth, customPaperHeight, dpi, (int)colorMode, copies, (int)duplex);
            if (res == NOT_OK) Debug.Log("Printer Plugin > SetPrinterSettings error" + CheckGdiError());
            return res;
        }

        public int ShowPrinterDialog()
        {
            int res = _ShowPrinterDialog();
			if (res == NOT_OK) Debug.Log("Printer Plugin > ShowPrinterDialog error" + CheckGdiError());
            return res;
		}

        public void ResetPrinterSettings()
        {
            _ResetPrinterSettings();
        }

        public void GetPageSize(ref float width_mm, ref float height_mm)
        {
            _GetPageSize(ref width_mm, ref height_mm);
        }

        public string GetLastGdiStatus()
        {
            int status = _GetLastGdiStatus();
            switch (status)
            {
                case 0: return "Ok";
                case 1: return "GenericError";
                case 2: return "InvalidParameter";
                case 3: return "OutOfMemory";
                case 4: return "ObjectBusy";
                case 5: return "InsufficientBuffer";
                case 6: return "NotImplemented";
                case 7: return "Win32Error";
                case 8: return "WrongState";
                case 9: return "Aborted";
                case 10: return "FileNotFound";
                case 11: return "ValueOverflow";
                case 12: return "AccessDenied";
                case 13: return "UnknownImageFormat";
                case 14: return "FontFamilyNotFound";
                case 15: return "FontStyleNotFound";
                case 16: return "NotTrueTypeFont";
                case 17: return "UnsupportedGdiplusVersion";
                case 18: return "GdiplusNotInitialized";
                case 19: return "PropertyNotFound";
                case 20: return "PropertyNotSupported";
                default: return "Unknown Error";
            }
        }

        private string CheckGdiError()
        {
			int status = _GetLastGdiStatus();
            if (status > 0) return " - System error: " + GetLastGdiStatus();
            else return "";
		}

        public void StartDocument()
        {
            int res = _StartDocument();
            if (res == NOT_OK) Debug.Log("Printer Plugin > StartDocument error" + CheckGdiError());
        }

        /// <summary>
        /// Set current print position, in millimeters, from the left and top borders of the page
        /// </summary>
        public void SetPrintPosition(float left_mm, float top_mm)
        {
            _SetPrintPosition(left_mm, top_mm);
        }

        public void NewPage()
        {
            int res = _NewPage();
            if (res == NOT_OK) Debug.Log("Printer Plugin > NewPage error" + CheckGdiError());
        }

        public void EndDocument()
        {
            int res = _EndDocument();
            if (res == NOT_OK) Debug.Log("Printer Plugin > EndDocument error" + CheckGdiError());
        }

        public int PrintTexture(Texture2D texture, float width_mm, float height_mm)
        {
            if (width_mm <= 0 && height_mm <= 0) return NOT_OK;
            int res = NOT_OK;
            byte[] textureBuffer = null;
            IntPtr bufferPtr = IntPtr.Zero;
            try
            {
                if (showDebug) Debug.Log("PrintTexture > Encoding png");
                textureBuffer = texture.EncodeToPNG();
                if (showDebug) Debug.Log("PrintTexture > Allocating buffer, length = " + textureBuffer.Length);
                bufferPtr = Marshal.AllocHGlobal(textureBuffer.Length);
                // copy buffer, needed because we will now call unmanaged code
                if (showDebug) Debug.Log("PrintTexture > Copying buffer");
                Marshal.Copy(textureBuffer, 0, bufferPtr, textureBuffer.Length);
                if (showDebug) Debug.Log("PrintTexture > Printing...");
                res = _PrintImageFromStream(bufferPtr, (UInt32)textureBuffer.Length, width_mm, height_mm);
                if (res == NOT_OK) Debug.Log("Printer Plugin > Errors printing texture" + CheckGdiError());
            }
            catch
            {
                Debug.LogError("Printer Plugin > errors printing texture");
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero) Marshal.FreeHGlobal(bufferPtr);
                textureBuffer = null;
            }
            return res;
        }
       
        public int PrintImageFromFile(string path, float width_mm, float height_mm)
        {
            int res = _PrintImageFromFile(Marshal.StringToHGlobalUni(path), width_mm, height_mm);
            if (res == NOT_OK) Debug.Log("Printer Plugin > Errors printing image" + CheckGdiError());
			return res;
        }

        public int PrintText(string text, float width_mm = 0, float height_mm = 0,
            TextHorizAlignment alignment = TextHorizAlignment.Left, TextVertAlignment vertAlignment = TextVertAlignment.Top)
        {
            int res = _PrintText(Marshal.StringToHGlobalUni(text), width_mm, height_mm, (int)alignment, (int)vertAlignment);
            if (res == NOT_OK) Debug.Log("Printer Plugin > Errors printing text" + CheckGdiError());
			return res;
        }

        [Obsolete("Use the method with TextHorizAlignment and TextVertAlignment instead")]
        public int PrintText(string text, float width_mm, float height_mm, TextAlignment alignment)
        {
            return PrintText(text, width_mm, height_mm, (TextHorizAlignment)alignment, TextVertAlignment.Top);
        }

        public void SetTextColor(Color color)
        {
            _SetTextColor(
                Mathf.RoundToInt(color.r * 255) % 256,
                Mathf.RoundToInt(color.g * 255) % 256,
                Mathf.RoundToInt(color.b * 255) % 256);
        }

        /// <summary>
        /// Set the size (height) of the font in millimeters
        /// </summary>
        public void SetTextFontSize(float size_mm)
        {
            if (size_mm <= 0) return;
            _SetTextFontSize(size_mm);
        }

        /// <summary>
        /// Set a font familiy, i.e. "Arial" or "Times New Roman"
        /// </summary>
        public void SetTextFontFamily(string name)
        {
            _SetTextFontFamily(Marshal.StringToHGlobalUni(name));
        }

        public void SetTextFontStyle(TextFontStyle style)
        {
            _SetTextFontStyle((int)style);
        }
    }
}