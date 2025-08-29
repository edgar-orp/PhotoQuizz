using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using PrintLib;

public class PrintDemo : MonoBehaviour
{
    Printer printer; // our main printer object
    List<string> printers; // used to save printer names
    Dropdown printerMenu;
    RawImage image;
    Button printBtn;
    InputField field;
    public Texture2D printTestImage;

    void Start()
    {
        // create printer object
        printer = new Printer();
        // get controls and set events
        printBtn = gameObject.transform.Find("PrintBtn").GetComponent<Button>();
        printBtn.onClick.AddListener(Print);
        printerMenu = gameObject.transform.Find("PrinterMenu").GetComponent<Dropdown>();
        printerMenu.onValueChanged.AddListener(ChangePrinter);
        image = gameObject.transform.Find("Image").GetComponent<RawImage>();
        image.texture = printTestImage;
        GameObject obj = GameObject.Find("/Canvas/Win/ScrollView/Viewport/Content/InputField");
        if (obj) field = obj.GetComponent<InputField>();
        // enum all printers
        printerMenu.ClearOptions();
        int printerCount = printer.GetPrinterCount();
        printers = new List<string>();
        for (int i = 0; i < printerCount; i++)
        {
            string printerName = printer.GetPrinterName(i);
            printerMenu.options.Add(new Dropdown.OptionData(printerName));
            printers.Add(printerName);
        }
        // select the default printer
        if (printerCount > 0)
        {
            string defaultPrinter = printer.GetDefaultPrinterName();
            int index = 0;
            // find the index of the default printer
            if (!string.IsNullOrEmpty(defaultPrinter)) index = printers.IndexOf(defaultPrinter);
            // select this printer
            printerMenu.SetValueWithoutNotify(index);
            printerMenu.RefreshShownValue();
            ChangePrinter(index);
        }
    }

    void ChangePrinter(int index)
    {
        string printerName = printer.GetPrinterName(index);
		printer.SelectPrinter(printerName);
	}

    // start the print test
    void Print()
    {
        // change printer settings here!
        printer.SetPrinterSettings(Orientation.Default, PaperFormat.Default, 0, ColorMode.Default, 1);

        printer.StartDocument();

        // print some text, start 2cm from the left border and 4cm from the top border
        printer.SetPrintPosition(20, 40);
        printer.SetTextFontFamily("Arial");
        printer.SetTextFontSize(4); // mm
        printer.SetTextColor(Color.black);
        printer.SetTextFontStyle(TextFontStyle.Regular);
        printer.PrintText("Text printing test, Arial font, size 4mm");

        // second row, change font, size and color
        printer.SetPrintPosition(20, 50);
        printer.SetTextFontFamily("Times New Roman");
        printer.SetTextFontSize(6); // mm
        printer.SetTextColor(Color.red);
        printer.PrintText("Times New Roman font, size 6mm, red");

        // third row, same font attributes, but blue and bold
        printer.SetPrintPosition(20, 63);
        printer.SetTextColor(Color.blue);
        printer.SetTextFontStyle(TextFontStyle.Bold);
        printer.PrintText("Times New Roman font, size 6mm, blue, bold");

        // image, you can edit printTestImage inside Canvas/Win (texture must have Read/Write flag enabled)
        printer.SetPrintPosition(80, 90);
        // print the image with a size of 5x5 cm
        printer.PrintTexture(printTestImage, 50, 50);

        // if we specify a width and a height for the text, the text will be wrapped in a rectangle
        printer.SetTextFontFamily("Verdana");
        printer.SetTextFontSize(4);
        printer.SetTextColor(new Color(0.2f, 0.2f, 0.2f));
        printer.SetTextFontStyle(TextFontStyle.Regular);
        // the print position become the rect's top-left
        printer.SetPrintPosition(20, 160);
        // and here we set the text and we define the rect's width-height (170mm x 100mm)
        printer.PrintText(field.text, 170, 100);

        // let's print a title at the top of the page
        // this to show that we don't necessarily need to draw elements in the top-down order
        printer.SetPrintPosition(20, 15);
        printer.SetTextFontSize(8);
        printer.PrintText("Printer Plugin");

        // end document and send to printer!
        printer.EndDocument();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }
}
