using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DeepReality.Subscription;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using Unity.Barracuda;
using System.Reflection;
using DeepReality.Internal.Data;

namespace DeepReality.Subscription.Inspectors{
    [CustomEditor(typeof(SubscriptionModelProcessing))]
    public class SubscriptionInspector : Editor
    {

        SerializedProperty availableLabelsProperty;
        SerializedProperty modelFileProperty;
        SerializedProperty keyProperty;
        SerializedProperty modelTypeProperty;
        SerializedProperty configProperty;
        bool downloading = false;

        SubscriptionModelProcessing modelProcessing;

        GUIStyle myTextAreaStyle = new GUIStyle(EditorStyles.textArea);

        void OnEnable()
        {
            keyProperty = serializedObject.FindProperty("subscriptionKey");
            availableLabelsProperty = serializedObject.FindProperty(nameof(SubscriptionModelProcessing.availableLabels));
            modelFileProperty = serializedObject.FindProperty("modelFile");
            modelTypeProperty = serializedObject.FindProperty("modelType");
            configProperty = serializedObject.FindProperty("config");

            myTextAreaStyle.wordWrap = true;
        }

        public override void OnInspectorGUI()
        {
            
            serializedObject.Update();

            modelProcessing = (SubscriptionModelProcessing)target;


            //EditorGUILayout.PropertyField(modelTypeProperty);


            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Subscription Key");
            keyProperty.stringValue = EditorGUILayout.TextArea(keyProperty.stringValue,myTextAreaStyle, GUILayout.MinHeight(100));
            GUILayout.EndVertical();

            string downloadButtonTitle = "DOWNLOAD DATA";

            if (availableLabelsProperty.arraySize == 0 || modelFileProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Model data unavailable! Please download the data with the button below", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Model data ready!", MessageType.Info);
                downloadButtonTitle = "UPDATE DATA";
            }

            if (GUILayout.Button(downloadButtonTitle))
            {
                DownloadData(typeof(NNModel));
            }


            if (availableLabelsProperty.arraySize > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("Available labels:");
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Space(5);
                if (modelProcessing.availableLabels != null)
                {
                    for (int i = 0; i < modelProcessing.availableLabels.Length; i++)
                    {
                        SerializedProperty labelEnabledProperty = serializedObject.FindProperty($"availableLabels.Array.data[{i}].enabled");

                        GUILayout.BeginHorizontal();
                        labelEnabledProperty.boolValue = EditorGUILayout.Toggle(labelEnabledProperty.boolValue, GUILayout.Width(40));
                        GUILayout.Label(modelProcessing.availableLabels[i].label);
                        GUILayout.EndHorizontal();
                        DrawUILine(Color.gray);
                    }
                }

                GUILayout.Space(5f);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("ENABLE ALL",GUILayout.MinHeight(30)))
                {
                    SetAllEnabled(true);
                }
                if (GUILayout.Button("DISABLE ALL", GUILayout.MinHeight(30)))
                {
                    SetAllEnabled(false);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

        }

        private async void DownloadData(Type modelType) {
            if (downloading || string.IsNullOrWhiteSpace(keyProperty.stringValue)) return;


            bool success = false;
            string localFilePath = "";
            try
            {
                downloading = true;
                
                AssetDatabase.StartAssetEditing();
                EditorUtility.DisplayProgressBar("Downloading data", "Downloading the model linked to the subscription key...", 0f);

                var config = await DeepReality.Internal.Editor.InspectorUtilities.GetConfigurationAsync(keyProperty.stringValue);

                availableLabelsProperty.ClearArray();
                availableLabelsProperty.arraySize = config.labels.Count;
                for (int i = 0; i < config.labels.Count; i++)
                {
                    SetPropertyValue(availableLabelsProperty.GetArrayElementAtIndex(i), new AvailableLabel { label = config.labels[i], enabled = true });
                }

                modelTypeProperty.stringValue = config.modelType;
                configProperty.stringValue = config.config;
                serializedObject.ApplyModifiedProperties();

                localFilePath = await DeepReality.Internal.Editor.InspectorUtilities.DownloadModelAsync(keyProperty.stringValue, config, p =>
                {
                    EditorUtility.DisplayProgressBar("Downloading data", "Downloading the model linked to the subscription key...", p);
                });
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(localFilePath);
                success = true;

            }
            catch (UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog("ERROR", "Invalid subscription key.", "Close");
                success = false;
            }
            catch (Exception e)
            {
                //Debug.Log(e);
                EditorUtility.DisplayDialog("ERROR", "Unable to download model data. Please check your internet connection and try again.", "Close");
                Debug.Log(e.Message);
                success = false;
                //Debug.Log(e.StackTrace);
            }
            finally
            {
                downloading = false;
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            if(success && !string.IsNullOrWhiteSpace(localFilePath))
            {
                modelFileProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath(localFilePath, modelType);
                serializedObject.ApplyModifiedProperties();
            }

            
        }


        static void SetPropertyValue(UnityEditor.SerializedProperty property, AvailableLabel label)
        {
            property.Next(true);
            property.stringValue = label.label;
            property.Next(false);
            property.boolValue = label.enabled;
        }

        void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        void SetAllEnabled(bool enabled)
        {
            for (int i = 0; i < modelProcessing.availableLabels.Length; i++)
            {
                SerializedProperty labelEnabledProperty = serializedObject.FindProperty($"availableLabels.Array.data[{i}].enabled");

                labelEnabledProperty.boolValue = enabled;
            }
        }

        [MenuItem("GameObject/DeepReality/DeepReality Subscription Model Processing")]
        private static void AddDeepRealitySession(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new GameObject("DeepRealitySubscription");
            SubscriptionModelProcessing sm = go.AddComponent<SubscriptionModelProcessing>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
    
}
