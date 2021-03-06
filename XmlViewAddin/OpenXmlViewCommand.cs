// --------------------------------------------------------------------
// © Copyright 2013 Hewlett-Packard Development Company, L.P.
//--------------------------------------------------------------------

using System;
using HP.Utt.UttCore;
using HP.Utt.CodeEditorUtils;
using ICSharpCode.SharpDevelop.Editor;
using System.Text;
using System.Xml;
using HP.Utt.UttDialog;
using HP.LR.VuGen.XmlViewer;
using System.Xml.Linq;

namespace XmlViewAddin
{
  public class OpenXmlViewCommand : UttBaseWpfCommand
  {
    public static bool IsValidXml(string xml)
    {
      if (string.IsNullOrWhiteSpace(xml))
      {
        return false;
      }

      XmlDocument doc = new XmlDocument();
      try
      {
        doc.LoadXml(xml);
      }
      catch
      {
        return false;
      }

      return true;
    }

    public static string GetSelectedString()
    {
      ITextEditor editor = UttCodeEditor.GetActiveTextEditor();
      if (editor == null)
        return null;
      string[] lines = editor.SelectedText.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      StringBuilder result = new StringBuilder();
      foreach (string line in lines)
      {
        string processedLine = line.Trim();
        //Remove the leading and trailing " - this is VuGen specific code
        if (processedLine.StartsWith("\""))
        {
          processedLine = processedLine.Remove(0, 1);
        }
        if (processedLine.EndsWith("\""))
        {
          processedLine = processedLine.Remove(processedLine.Length - 1, 1);
        }
        processedLine = processedLine.Replace("\\", "");

        result.Append(processedLine);
      }

      return result.ToString();
    }

    public override void Run()
    {
      string xml = GetSelectedString();
      if (IsValidXml(xml))
      {
        ShowXmlDialog(xml);
      }
    }

    private void ShowXmlDialog(string xml)
    {
      CustomDialog dialog = new CustomDialog();
      XmlViewSingleContent content = new XmlViewSingleContent();
      SingleDirectionData data = new SingleDirectionData();
      content.DataContext = data;
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      data.Document = doc;
      dialog.Content = content;
      dialog.MaxWidth = 800;
      dialog.AddOkButton();
      dialog.AddButton("reformat", Reformat, System.Windows.Input.Key.R, System.Windows.Input.ModifierKeys.Alt, "Reformat");
      dialog.Show();
    }

    private void Reformat(CustomDialog dialog)
    {
      ITextEditor editor = UttCodeEditor.GetActiveTextEditor();
      string selectedText = editor.SelectedText;
      string[] xmlLines = FormatXml(GetSelectedString()).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      IDocumentLine documentLine = editor.Document.GetLineForOffset(editor.SelectionStart);
      string lineText = documentLine.Text;
      string indentation = lineText.Substring(0, lineText.Length - lineText.TrimStart().Length);

      StringBuilder formattedText = new StringBuilder();
      foreach (string line in xmlLines)
      {
        formattedText.AppendLine(indentation+"\""+line.Replace("\"", "\\\"")+"\"");
      }

      formattedText.Remove(formattedText.Length - Environment.NewLine.Length, Environment.NewLine.Length);

      if (!selectedText.TrimStart().StartsWith("\""))
      {
        formattedText.Insert(0, "\"" + Environment.NewLine);
      }

      if (!selectedText.TrimEnd().EndsWith("\""))
      {
        formattedText.Append(Environment.NewLine+indentation+"\"");
      }

		
      DocumentUtilitites.FindNextWordStart(editor.Document, documentLine.Offset);
      
      editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, formattedText.ToString());
      dialog.Close();
    }

    private string FormatXml(String xml)
    {
      try
      {
        XDocument doc = XDocument.Parse(xml);
        return doc.ToString();
      }
      catch (Exception)
      {
        return xml;
      }
    }
  }
}
