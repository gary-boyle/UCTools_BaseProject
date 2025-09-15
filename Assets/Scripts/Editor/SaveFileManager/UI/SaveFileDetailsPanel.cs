// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using GameFramework.DataStructures;
// using GameFramework.Editor.SaveFileManager.ScriptableObjects;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace GameFramework.Editor.SaveFileManager.UI
// {
//     /// <summary>
//     /// Panel for displaying detailed information about a selected save file
//     /// Uses reflection-based field display for flexibility with changing save structures
//     /// </summary>
//     public class SaveFileDetailsPanel : VisualElement
//     {
//         private const string UXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/SaveFileDetailsPanel.uxml";
//         private const string FieldItemUXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/FieldDisplayItem.uxml";
//         
//         // UI References
//         private VisualElement _noSelection;
//         private VisualElement _detailsContent;
//         private Button _loadButton;
//         private Button _deleteButton;
//         private Button _showExplorerButton;
//         private VisualElement _playModeWarning;
//         private ScrollView _fieldsContainer;
//         private VisualElement _rawDataSection;
//         private ScrollView _rawDataContainer;
//         private Label _rawDataContent;
//         
//         // Data
//         private SaveFileInfo_old _currentSave;
//         private ScriptableObjects.SaveFileDisplayConfig _displayConfig;
//         private VisualTreeAsset _fieldItemTemplate; 
//         
//         // Events
//         public event Action<SaveFileInfo_old> OnLoadRequested;
//         public event Action<SaveFileInfo_old> OnDeleteRequested;
//         public event Action<SaveFileInfo_old> OnShowInExplorerRequested;
//         
//         public SaveFileDetailsPanel()
//         {
//             LoadUI();
//         }
//         
//         private void LoadUI()
//         {
//             var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
//             if (uxml == null)
//             {
//                 Debug.LogError($"Could not load UXML file at {UXMLPath}");
//                 return;
//             }
//             
//             // Load field item template
//             _fieldItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FieldItemUXMLPath);
//             if (_fieldItemTemplate == null)
//             {
//                 Debug.LogError($"Could not load field item template at {FieldItemUXMLPath}");
//                 return;
//             }
//             
//             uxml.CloneTree(this);
//             
//             GetUIReferences();
//             SetupEventHandlers();
//             UpdatePlayModeWarning();
//         }
//         
//         private void GetUIReferences()
//         {
//             _noSelection = this.Q("no-selection");
//             _detailsContent = this.Q("details-content");
//             _loadButton = this.Q<Button>("load-button");
//             _deleteButton = this.Q<Button>("delete-button");
//             _showExplorerButton = this.Q<Button>("show-explorer-button");
//             _playModeWarning = this.Q("play-mode-warning");
//             _fieldsContainer = this.Q<ScrollView>("fields-container");
//             _rawDataSection = this.Q("raw-data-section");
//             _rawDataContainer = this.Q<ScrollView>("raw-data-container");
//             _rawDataContent = this.Q<Label>("raw-data-content");
//         }
//         
//         private void SetupEventHandlers()
//         {
//             _loadButton.clicked += () => OnLoadRequested?.Invoke(_currentSave);
//             _deleteButton.clicked += () => OnDeleteRequested?.Invoke(_currentSave);
//             _showExplorerButton.clicked += () => OnShowInExplorerRequested?.Invoke(_currentSave);
//         }
//         
//         public void LoadSaveFile(SaveFileInfo_old saveInfoOld, ScriptableObjects.SaveFileDisplayConfig displayConfig, bool showRawData)
//         {
//             _currentSave = saveInfoOld;
//             _displayConfig = displayConfig;
//             
//             if (saveInfoOld == null)
//             {
//                 ShowNoSelection();
//                 return;
//             }
//             
//             ShowDetailsContent();
//             UpdateFieldsDisplay();
//             UpdateRawDataDisplay(showRawData);
//             UpdatePlayModeWarning();
//         }
//         
//         private void ShowNoSelection()
//         {
//             _noSelection.style.display = DisplayStyle.Flex;
//             _detailsContent.style.display = DisplayStyle.None;
//         }
//         
//         private void ShowDetailsContent()
//         {
//             _noSelection.style.display = DisplayStyle.None;
//             _detailsContent.style.display = DisplayStyle.Flex;
//         }
//         
//         private void UpdateFieldsDisplay()
//         {
//             _fieldsContainer.Clear();
//             
//             if (_currentSave == null) return;
//             
//             if (_displayConfig?.DisplayFields != null && _displayConfig.DisplayFields.Count > 0)
//             {
//                 // Use configured fields
//                 foreach (var fieldConfig in _displayConfig.DisplayFields)
//                 {
//                     CreateFieldDisplay(fieldConfig.FieldName, fieldConfig.DisplayName, fieldConfig.IsReadOnly);
//                 }
//             }
//             else
//             {
//                 // Use default fields
//                 CreateDefaultFieldDisplays();
//             }
//         }
//         
//         private void CreateDefaultFieldDisplays()
//         {
//             CreateFieldDisplay(nameof(SaveFileInfo_old.FileName), "File Name");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.PlayerName), "Player Name");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.Difficulty), "Difficulty");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.CurrentScene), "Current Scene");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.PlayerLevel), "Player Level");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.Score), "Score");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.FormattedPlayTime), "Play Time");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.FormattedDate), "Last Save");
//             CreateFieldDisplay(nameof(SaveFileInfo_old.IsAutoSave), "Auto Save");
//         }
//         
//         private void CreateFieldDisplay(string fieldName, string displayName, bool isReadOnly = true)
//         {
//             try
//             {
//                 var fieldItem = _fieldItemTemplate.CloneTree();
//                 var fieldLabel = fieldItem.Q<Label>("field-label");
//                 fieldLabel.text = displayName;
//                 
//                 // Get field value using reflection
//                 var field = typeof(SaveFileInfo_old).GetField(fieldName);
//                 var property = typeof(SaveFileInfo_old).GetProperty(fieldName);
//                 
//                 object value = null;
//                 Type fieldType = null;
//                 
//                 if (field != null)
//                 {
//                     value = field.GetValue(_currentSave);
//                     fieldType = field.FieldType;
//                 }
//                 else if (property != null)
//                 {
//                     value = property.GetValue(_currentSave);
//                     fieldType = property.PropertyType;
//                 }
//                 else
//                 {
//                     // Field not found, show error
//                     var textField = fieldItem.Q<TextField>("field-value-text");
//                     textField.style.display = DisplayStyle.Flex;
//                     textField.value = "Field not found";
//                     textField.SetEnabled(false);
//                     _fieldsContainer.Add(fieldItem);
//                     return;
//                 }
//                 
//                 // Display appropriate control based on type
//                 DisplayFieldValue(fieldItem, value, fieldType, isReadOnly);
//                 
//                 _fieldsContainer.Add(fieldItem);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error creating field display for {fieldName}: {ex.Message}");
//             }
//         }
//         
//         private void DisplayFieldValue(VisualElement fieldItem, object value, Type fieldType, bool isReadOnly)
//         {
//             // Hide all field value controls first
//             fieldItem.Q("field-value-text").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-int").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-float").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-bool").style.display = DisplayStyle.None;
//             
//             if (fieldType == typeof(int))
//             {
//                 var intField = fieldItem.Q<IntegerField>("field-value-int");
//                 intField.style.display = DisplayStyle.Flex;
//                 intField.value = (int)(value ?? 0);
//                 intField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(float))
//             {
//                 var floatField = fieldItem.Q<FloatField>("field-value-float");
//                 floatField.style.display = DisplayStyle.Flex;
//                 floatField.value = (float)(value ?? 0f);
//                 floatField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(bool))
//             {
//                 var boolField = fieldItem.Q<Toggle>("field-value-bool");
//                 boolField.style.display = DisplayStyle.Flex;
//                 boolField.value = (bool)(value ?? false);
//                 boolField.SetEnabled(!isReadOnly);
//             }
//             else
//             {
//                 // Default to text field for everything else
//                 var textField = fieldItem.Q<TextField>("field-value-text");
//                 textField.style.display = DisplayStyle.Flex;
//                 textField.value = value?.ToString() ?? "null";
//                 textField.SetEnabled(!isReadOnly);
//             }
//         }
//         
//         private void UpdateRawDataDisplay(bool showRawData)
//         {
//             _rawDataSection.style.display = showRawData ? DisplayStyle.Flex : DisplayStyle.None;
//             
//             if (!showRawData || _currentSave == null) return;
//             
//             try
//             {
//                 var savePath = GetSaveFilePath(_currentSave.FileName);
//                 if (File.Exists(savePath))
//                 {
//                     var jsonContent = File.ReadAllText(savePath);
//                     _rawDataContent.text = jsonContent;
//                 }
//                 else
//                 {
//                     _rawDataContent.text = "Save file not found";
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _rawDataContent.text = $"Error reading save file: {ex.Message}";
//             }
//         }
//         
//         private void UpdatePlayModeWarning()
//         {
//             _playModeWarning.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
//             _loadButton.SetEnabled(Application.isPlaying);
//         }
//         
//         private string GetSaveFilePath(string fileName)
//         {
//             return Application.persistentDataPath + "/Saves/" + fileName + ".gamesave";
//         }
//     }
// }
