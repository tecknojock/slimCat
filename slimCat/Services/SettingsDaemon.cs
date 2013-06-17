﻿/*
Copyright (c) 2013, Justin Kadrovach
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL JUSTIN KADROVACH BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Services
{
    public static class SettingsDaemon
    {
        const string SETTINGS_FILE_NAME = "!settings.xml";

        private static void makeSettingsFileIfNotExist(string CurrentCharacter, string Title, string ID, Models.ChannelType chanType)
        {
            var path = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
            { // make a new XML settings document
                var newSettings = new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm ? true : false);
                SerializeObjectToXML(newSettings, workingPath);
            }
        }

        private static void makeGlobalSettingsFileIfNotExist(string currentCharacter)
        {
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var workingPath = Path.Combine(path, SETTINGS_FILE_NAME);

            if (!File.Exists(workingPath))
                SaveApplicationSettingsToXML(currentCharacter);
        }

        /// <summary>
        /// Returns either the channel settings that already exist or a new settings file
        /// </summary>
        public static Models.ChannelSettingsModel GetChannelSettings(string CurrentCharacter, string Title, String ID, Models.ChannelType chanType)
        {
            makeSettingsFileIfNotExist(CurrentCharacter, Title, ID, chanType);
            var workingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            try
            {
                return ReadObjectFromXML<Models.ChannelSettingsModel>(workingPath, new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm? true : false)); // try and parse the XML file
            }

            catch
            {
                return new Models.ChannelSettingsModel(chanType == Models.ChannelType.pm ? true : false); // return a default if it's not legible
            }
        }

        /// <summary>
        /// Serialize and object to XML through reflection
        /// </summary>
        public static void SerializeObjectToXML(object toSerialize, string fileName, bool encrypt = false)
        {
            Type type = toSerialize.GetType();
            string[] checkTerms = new string[] { "command", "is", "enumerable" };
            XElement root = new XElement("settings");

            foreach (var property in type.GetProperties())
            {
                if (!checkTerms.Any(term => property.Name.ToLower().Contains(term)))
                root.Add(
                    new XElement(property.Name, property.GetValue(toSerialize, null)) // reflect its name and value, then write
                    );
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            if (encrypt)
                File.Encrypt(fileName);

            root = null;
        }

        /// <summary>
        /// Return type T from a specified XML file, using reflection
        /// </summary>
        public static T ReadObjectFromXML<T>(string fileName, T baseObject, bool decrypt = false)
            where T : new()
        {
            Type type = baseObject.GetType();
            var propertyList = type.GetProperties(); // reflect property names

            if (decrypt)
                File.Decrypt(fileName);

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) // open our file
            {
                var workingElement = XElement.Load(fs);
                foreach (var element in workingElement.Descendants()) // iterate through each element
                {
                    foreach (var property in propertyList) // check if the element is one of our properties
                    {
                        if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                        {
                            var setter = Convert.ChangeType(element.Value, property.PropertyType);
                            property.SetValue(baseObject, setter, null);
                            break;
                        }
                    }
                }
            }

            return baseObject; // return it
        }

        /// <summary>
        /// Updates the application settings from file
        /// </summary>
        public static void ReadApplicationSettingsFromXML(string currentCharacter)
        {
            makeGlobalSettingsFileIfNotExist(currentCharacter);

            Type type = typeof(Models.ApplicationSettings);
            var propertyList = type.GetProperties();
            var path = StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global");
            path = Path.Combine(path, SETTINGS_FILE_NAME);

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var workingElement = XElement.Load(fs);
                    foreach (var element in workingElement.Descendants())
                    {
                        foreach (var property in propertyList)
                        {
                            if (string.Equals(property.Name, element.Name.ToString(), StringComparison.Ordinal))
                            {
                                if (string.IsNullOrWhiteSpace(element.Value))
                                    continue; // fix a bad issue with the parser

                                if (!element.HasElements)
                                {
                                    var setter = Convert.ChangeType(element.Value, property.PropertyType);
                                    property.SetValue(null, setter, null);
                                    break;
                                }
                                else
                                {
                                    var collection = Models.ApplicationSettings.SavedChannels;

                                    if (property.Name.Equals("interested", StringComparison.OrdinalIgnoreCase))
                                        collection = Models.ApplicationSettings.Interested;

                                    else if (property.Name.Equals("notinterested", StringComparison.OrdinalIgnoreCase))
                                        collection = Models.ApplicationSettings.NotInterested;

                                    collection.Clear();
                                    foreach (var item in element.Elements())
                                        collection.Add(item.Value);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Updates the application settings file from memory
        /// </summary>
        public static void SaveApplicationSettingsToXML(string currentCharacter)
        {
            XElement root = new XElement("settings");
            var fileName = Path.Combine(StaticFunctions.MakeSafeFolderPath(currentCharacter, "Global", "Global"), SETTINGS_FILE_NAME);

            foreach (var property in typeof(Models.ApplicationSettings).GetProperties())
            {
                if (property.PropertyType != typeof(IList<string>) && property.PropertyType != typeof(IEnumerable<string>))
                    root.Add(
                        new XElement(property.Name, property.GetValue(null, null))
                        );
                else
                {
                    if (!property.Name.ToLower().Contains("list"))
                    {
                        var toAdd = new XElement(property.Name);
                        foreach (var item in property.GetValue(null, null) as IEnumerable<string>)
                        {
                            var label = "item";
                            if (property.Name.ToLower().Contains("channel"))
                                label = "channel";
                            else if (property.Name.ToLower().Contains("interested"))
                                label = "character";
                            toAdd.Add(new XElement(label, item));
                        }
                        root.Add(toAdd);
                    }
                }
            }

            File.Delete(fileName);
            using (var fs = File.OpenWrite(fileName))
                root.Save(fs);

            root = null;
        }

        /// <summary>
        /// Updates our settings XML file with newSettingsModel
        /// </summary>
        public static void UpdateSettingsFile(object newSettingsModel, string CurrentCharacter, string Title, string ID)
        {
            var workingPath = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);
            workingPath = Path.Combine(workingPath, SETTINGS_FILE_NAME);

            SerializeObjectToXML(newSettingsModel, workingPath);
        }

        public static bool HasChannelSettings(string CurrentCharacter, string Title, string ID)
        {
            var path = StaticFunctions.MakeSafeFolderPath(CurrentCharacter, Title, ID);

            return Directory.Exists(Path.Combine(path, SETTINGS_FILE_NAME));
        }
    }
}
