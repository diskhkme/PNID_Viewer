﻿using Microsoft.Win32;
using PNID_Viewer.Model;
using PNID_Viewer.ViewModel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace PNID_Viewer.ViewModel
{
    //TODO: 표 수정시 ctrl + Z 기능이 없음
    public class ViewerVM : Canvas
    {
        public XmlModel XmlModel { get; set; }
        public FilePathModel FilePathModel { get; set; }

        //XmlPathList는 이미 열린 파일인지 확인을 위한 것, XmlFileNameList은 리스트에 바인딩하기 위한 것
        public BindingList<string> XmlPathList { get; set; }
        public ObservableCollection<string> XmlFileNameList { get; set; }

        //파일을 열면 XmlDatas에 전부 추가됨, 리스트에서 체크된 파일만 CheckedXmlDatas에 추가되어 화면에 보임
        public ObservableCollection<XmlModel> XmlDatas { get; set; }
        public ObservableCollection<XmlModel> CheckedXmlDatas { get; set; }
        public ObservableCollection<XmlModel> TempXmlDatas { get; set; }

        //순서대로 xml을 열고, xml파일을 내보내고, 리스트 체크 여부를 알기 위한 것
        public OpenXmlCommand OpenXmlCommand { get; set; }
        public WriteXmlCommand WriteXmlCommand { get; set; }
        public IsCheckedCommand IsCheckedCommand { get; set; }

        //생성자
        public ViewerVM()
        {
            XmlModel = new XmlModel();
            FilePathModel = new FilePathModel();

            XmlPathList = new BindingList<string>();
            XmlFileNameList = new ObservableCollection<string>();

            XmlDatas = new ObservableCollection<XmlModel>();
            CheckedXmlDatas = new ObservableCollection<XmlModel>();

            OpenXmlCommand = new OpenXmlCommand(this);
            WriteXmlCommand = new WriteXmlCommand(this);
            IsCheckedCommand = new IsCheckedCommand(this);

            this.MouseLeftButtonDown += OnMouseLeftButtonDownCommand;
            this.MouseRightButtonDown += OnMouseRightButtonDownCommand;
        }

        Point start;
        Point end;

        public void OnMouseLeftButtonDownCommand(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                start = e.GetPosition((IInputElement)sender);
            }
        }

        public void OnMouseRightButtonDownCommand(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                end = e.GetPosition((IInputElement)sender);

                XmlModel temp = new XmlModel();
                temp.XmlFilename = CheckedXmlDatas[0].XmlFilename;
                temp.Xmax = (int)end.X;
                temp.Xmin = (int)start.X;
                temp.Ymax = (int)end.Y;
                temp.Ymin = (int)start.Y;

                if (temp.Xmax > temp.Xmin)
                    temp.RectangleWidth = temp.Xmax - temp.Xmin;
                else
                    temp.RectangleWidth = temp.Xmin - temp.Xmax;

                if (temp.Ymax > temp.Ymin)
                    temp.RectangleHeight = temp.Ymax - temp.Ymin;
                else
                    temp.RectangleHeight = temp.Ymin - temp.Ymax;
                CheckedXmlDatas.Add(temp);
            }
        }


        //XmlDatas에서 원하는 정보만을 CheckedXmlDatas에 추가/제거하는 함수
        //IsCheckedCommand에서 사용
        public void AddData(string _XmlFileName)
        {
            TempXmlDatas = new ObservableCollection<XmlModel>();
            //XmlDatas -> CheckedXmlDatas
            foreach (var item in XmlDatas)
            {
                if (item.XmlFilename.Equals(_XmlFileName))
                {
                    CheckedXmlDatas.Add(item);
                    TempXmlDatas.Add(item);
                }
            }
            foreach (var item in TempXmlDatas)
            {
                if (item.XmlFilename.Equals(_XmlFileName))
                {
                    XmlDatas.Remove(item);
                }
            }
        }
        public void DeleteData(string _XmlFileName)
        {
            TempXmlDatas = new ObservableCollection<XmlModel>();
            //CheckedXmlDatas -> XmlDatas
            foreach (var item in CheckedXmlDatas)
            {
                if (item.XmlFilename.Equals(_XmlFileName))
                {
                    XmlDatas.Add(item);
                    TempXmlDatas.Add(item);
                }
            }
            foreach (var item in TempXmlDatas)
            {
                if (item.XmlFilename.Equals(_XmlFileName))
                {
                    CheckedXmlDatas.Remove(item);
                }
            }
        }

        //Xml의 경로를 불러옴
        public void OpenXml()
        {
            FilePathModel.XmlPath = FileExplorer();
        }
        public void GetXmlDatas()
        {
            //xml파일만 진행됨(빈파일도 X)
            if (String.IsNullOrEmpty(FilePathModel.XmlPath) || (FilePathModel.XmlPath.Length <= 3)) return;
            else
            {
                string CheckXmlFile = FilePathModel.XmlPath.Substring(FilePathModel.XmlPath.Length - 3, 3);
                if (!CheckXmlFile.Equals("xml")) return;
            }

            //주의) 같은 파일이더라도 경로가 달라지면 다른 파일로 인지함
            if (XmlPathList.Count != 0)
            {
                for (int i = 0; i < XmlPathList.Count; i++)
                {
                    if (XmlPathList[i].Equals(FilePathModel.XmlPath))
                    {
                        MessageBox.Show("이미 열어본 파일입니다.");
                        return;
                    }
                }

                XmlPathList.Add(FilePathModel.XmlPath);
                XmlFileNameList.Add(FindNameToXmlPath(FilePathModel.XmlPath));
            }
            else
            {
                XmlPathList.Add(FilePathModel.XmlPath);
                XmlFileNameList.Add(FindNameToXmlPath(FilePathModel.XmlPath));
            }

            ReadXml(FilePathModel.XmlPath);


        }
        //Xml의 정보를 읽음
        private void ReadXml(string filePath)
        {
            //TODO해결: XmlDatas에 같은 게 들어감 -> DataGrid문제X, 모델 깊은 복사
            string _filename = "";
            string _width = "";
            string _height = "";
            string _depth = "";
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        //return only when you have START tag  
                        switch (reader.Name.ToString())
                        {
                            //TODO해결 : filename ~ depth는 1번만 들어감
                            case "filename":
                                _filename = reader.ReadString();
                                break;
                            case "width":
                                _width = reader.ReadString();
                                break;
                            case "height":
                                _height = reader.ReadString();
                                break;
                            case "depth":
                                _depth = reader.ReadString();
                                break;
                            case "name":
                                XmlModel.Name = reader.ReadString();
                                XmlModel.Filename = _filename;
                                XmlModel.Width = Convert.ToInt32(_width);
                                XmlModel.Height = Convert.ToInt32(_height);
                                XmlModel.Depth = Convert.ToInt32(_depth);

                                XmlModel.XmlFilename = FindNameToXmlPath(FilePathModel.XmlPath);

                                break;
                            case "degree":
                                XmlModel.Degree = Convert.ToDouble(reader.ReadString());
                                break;
                            case "xmin":
                                XmlModel.Xmin = Convert.ToInt32(reader.ReadString());
                                break;
                            case "ymin":
                                XmlModel.Ymin = Convert.ToInt32(reader.ReadString());
                                break;
                            case "xmax":
                                XmlModel.Xmax = Convert.ToInt32(reader.ReadString());
                                break;
                            case "ymax":
                                XmlModel.Ymax = Convert.ToInt32(reader.ReadString());

                                XmlModel.RectangleWidth = XmlModel.Xmax - XmlModel.Xmin;
                                XmlModel.RectangleHeight = XmlModel.Ymax - XmlModel.Ymin;

                                XmlModel temp = new XmlModel();
                                temp.XmlFilename = XmlModel.XmlFilename;
                                temp.Name = XmlModel.Name;
                                temp.Filename = XmlModel.Filename;
                                temp.Width = XmlModel.Width;
                                temp.Height = XmlModel.Height;
                                temp.Depth = XmlModel.Depth;
                                temp.Degree = XmlModel.Degree;
                                temp.Xmin = XmlModel.Xmin;
                                temp.Ymin = XmlModel.Ymin;
                                temp.Xmax = XmlModel.Xmax;
                                temp.Ymax = XmlModel.Ymax;
                                temp.RectangleWidth = XmlModel.RectangleWidth;
                                temp.RectangleHeight = XmlModel.RectangleHeight;

                                XmlDatas.Add(temp);
                                break;
                        }
                    }
                }
            }
        }
        public void WriteXml()
        {
            //주의) 1개만 체크되어있어야함.
            string str;
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Xml Files(*.xml;)|*.xml;|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                str = dialog.FileName;
                //MessageBox.Show(str);
            }
            else
            {
                //MessageBox.Show("!");
                return;
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = false
            };
            using (XmlWriter wr = XmlWriter.Create(str, settings))
            {
                wr.WriteStartDocument();
                wr.WriteStartElement("annotation");
                //TODO: CheckedXmlDatas에 1개의 xml정보만 들어와있어야함
                wr.WriteElementString("filmename", CheckedXmlDatas[0].Filename);
                wr.WriteStartElement("size");
                wr.WriteElementString("width", CheckedXmlDatas[0].Width.ToString());
                wr.WriteElementString("height", CheckedXmlDatas[0].Height.ToString());
                wr.WriteElementString("depth", CheckedXmlDatas[0].Depth.ToString());
                wr.WriteEndElement();  //size
                foreach (var item in CheckedXmlDatas)
                {
                    wr.WriteStartElement("object");
                    //TODO :name에 빈 값이 나올 때 오류 발생
                    wr.WriteElementString("name", item.Name);
                    wr.WriteElementString("degree", item.Degree.ToString());
                    wr.WriteStartElement("bndbox");
                    wr.WriteElementString("xmin", item.Xmin.ToString());
                    wr.WriteElementString("ymin", item.Ymin.ToString());
                    wr.WriteElementString("xmax", item.Xmax.ToString());
                    wr.WriteElementString("ymax", item.Ymax.ToString());
                    wr.WriteEndElement();  //bndbox
                    wr.WriteEndElement();  //object
                }
                wr.WriteEndElement();  //annotation
                wr.WriteEndDocument();
            }
        }

        //주의)이 함수는 xml 불러올 떄만 사용됨. image불러오는 함수는 OpenImageCommand에서 작성됨.
        private string FileExplorer()
        {
            OpenFileDialog dig = new OpenFileDialog();
            dig.Filter = "Xml Files(*.xml;)|*.xml;|All files (*.*)|*.*";
            bool? result = dig.ShowDialog();

            if (result == true) return dig.FileName;
            else return string.Empty;
        }

        //파일 경로 -> 파일 이름
        private string FindNameToXmlPath(string Path)
        {
            string[] words = Path.Split('\\');
            string lastWord = words[words.Length - 1];
            return lastWord;
        }

    }
}
