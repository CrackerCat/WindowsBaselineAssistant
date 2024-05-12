﻿using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;

namespace WindowsBaselineAssistant
{
    internal class Util
    {
        private static string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static string CurrentDirectory { get => currentDirectory; }
        public static string RoamingDirectory { get => $"{roamingDirectory}\\WindowsBaselineAssistant\\"; }

        private static string roamingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// 读xml文件
        /// </summary>
        /// <param name="xmlpath">xml文件路径</param>
        /// <returns>XmlDocument实例</returns>
        public static XmlDocument ReadXml(string xmlpath)
        {
            if (!File.Exists(xmlpath))
            {
                return null;
            }
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlpath);
            return xmlDocument;
        }

        /// <summary>
        /// 根据secedit中的key获取所属节和值
        /// </summary>
        /// <param name="mark">key</param>
        /// <returns>节,值</returns>
        public static (string, string) GetResultByMark(string mark)
        {
            IniFile ini = new IniFile(RoamingDirectory + "\\config.cfg");
            string[] sections = ini.Sections;
            foreach (var section in sections)
            {
                string value = ini.ReadString(section, mark, "");
                if (value != string.Empty)
                {
                    return (section, value);
                }
            }
            return ("", "");
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns>IP地址</returns>
        public static List<string> GetIPAddress()
        {
            List<string> ips = new List<string>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                    item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ips.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return ips;
        }

        /// <summary>
        /// 获取系统版本信息
        /// </summary>
        /// <returns>系统版本信息</returns>
        public static string GetOSVersion()
        {
            try
            {
                var osDescription = RuntimeInformation.OSDescription;
                var osArchitecture = RuntimeInformation.OSArchitecture;
                return $"{osDescription} {osArchitecture}";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return "获取系统信息失败";
            }
        }

        /// <summary>
        /// 判断XmlNode是否包含某一个键
        /// </summary>
        /// <param name="parentNode">xmlnode对象</param>
        /// <param name="elementName">包含的键</param>
        /// <returns>true: 包含 false：不包含</returns>
        public static bool ContainsElement(XmlNode parentNode, string elementName)
        {
            // 使用 SelectSingleNode 来查找指定名称的子节点
            XmlNode elementNode = parentNode.SelectSingleNode(elementName);

            // 判断是否找到了该节点
            return elementNode != null;
        }

        /// <summary>
        /// 执行外部程序
        /// </summary>
        /// <param name="command">参数</param>
        /// <param name="isnowindow">是否在新窗口中启动</param>
        /// <param name="processWindowStyle">窗口状态.默认:Normal</param>
        /// <returns>执行状态码</returns>
        public static int ExecutExternalProgram(string command, bool isnowindow, ProcessWindowStyle processWindowStyle = ProcessWindowStyle.Normal)
        {
            //要执行的命令
            //string command = "secedit /export /cfg config.cfg";
            // 创建一个新的进程启动信息
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = isnowindow,
                WindowStyle = processWindowStyle,
                Arguments = "/c " + command,
                WorkingDirectory = CurrentDirectory
            };
            // 创建进程对象
            Process process = new Process
            {
                StartInfo = processStartInfo
            };
            try
            {
                // 启动进程
                process.Start();
                process.WaitForExit();
                // 获取返回值
                int exitCode = process.ExitCode;
                return exitCode;
            }
            catch (Exception ex)
            {
                //UIMessageBox.ShowError(ex.Message);
                LogHelper.WriteLog(ex.Message.ToString(), ex);
                return -1;
            }
            finally
            {
                // 关闭进程
                process.Close();
                process.Dispose();
            }
        }

        /// <summary>
        ///检查所有文本框是否为空
        /// </summary>
        /// <param name="form">要检查的窗体名称</param>
        /// <param name="type">可选:检测类型[secedit],设置此值时跳过FieldTextBox检查</param>
        /// <returns>检查通过:true,反之false</returns>
        public static bool AreTextBoxesFilled(Form form, string type = null)
        {
            foreach (Control control in form.Controls)
            {
                if (control is UITextBox textBox)
                {
                    if (type.Equals("secedit") && textBox.Name.Equals("FieldTextBox"))
                    {
                        continue; // 如果type的值为secedit且文本框名为FieldTextBox，则跳过检查
                    }

                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // 方法用于添加item元素数据
        public static void AddItem(XmlDocument xmlDoc, XmlElement parentElement, Dictionary<string, string> itemData)
        {
            // 创建item元素
            XmlElement itemElement = xmlDoc.CreateElement("item");
            parentElement.AppendChild(itemElement);

            // 添加字典中的键值对作为子元素及其文本内容
            foreach (var pair in itemData)
            {
                AddXmlElement(xmlDoc, itemElement, pair.Key, pair.Value);
            }
        }

        // 辅助方法用于添加子元素及其文本内容
        private static void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string text)
        {
            XmlElement element = xmlDoc.CreateElement(elementName);
            element.InnerText = text;
            parentElement.AppendChild(element);
        }

        /*public static bool IsHomeEdition()
        {
            string edition = RegistryHelper.GetValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID");
            if (edition.Contains("Home"))
            {
                return true;
            }
            return false;
        }*/
    }
}
