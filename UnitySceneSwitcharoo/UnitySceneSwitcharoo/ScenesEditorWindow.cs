using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnitySceneSwitcharoo
{
    public class ScenesEditorWindow : EditorWindow
    {
        private static List<UnitySceneGroup> _unitySceneGroups = new List<UnitySceneGroup>();
        private static Vector2 _scrollPosition = Vector2.zero;
        private static GUIContent _searchButtonGUIContent = new GUIContent("Highlight this scene in the Project folder.");

        [MenuItem("Window/Unity Scene Switcharoo")]
        private static void Initialize()
        {
            var editorWindow = EditorWindow.GetWindow<ScenesEditorWindow>("Unity Scene Switcharoo");
            editorWindow.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Scene: " + EditorSceneManager.GetActiveScene().name, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!_unitySceneGroups.Any())
            {
                return;
            }

            if (_unitySceneGroups.Any(group => group.Foldout))
            {
                if (GUILayout.Button("Collapse All"))
                {
                    CollapseGroups();
                }
            }
            else
            {
                if (GUILayout.Button("Expand All"))
                {
                    ExpandGroups();
                }
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _unitySceneGroups.ForEach(unitySceneGroup =>
            {
                unitySceneGroup.Foldout = EditorGUILayout.Foldout(unitySceneGroup.Foldout, unitySceneGroup.GroupName);

                if (unitySceneGroup.Foldout)
                {
                    unitySceneGroup.UnityScenes.ForEach(unityScene =>
                    {
                        using (var horizontal = new EditorGUILayout.HorizontalScope())
                        {
                            GUI.backgroundColor = unityScene.IsActive ? Color.yellow : Color.white;
                            if (GUILayout.Button(unityScene.DisplayName))
                            {
                                OpenScene(unityScene.FullPath);
                            }

                            GUI.backgroundColor = Color.white;
                            if (GUILayout.Button(_searchButtonGUIContent, GUILayout.Width(18), GUILayout.Height(18)))
                            {
                                PingSceneInProject(unityScene.FullPath);
                            }
                        }
                    });
                }
                EditorGUILayout.Space();
            });
            EditorGUILayout.EndScrollView();
        }

        private void OnFocus()
        {
            UpdateScenes();
        }

        private void OnProjectChange()
        {
            UpdateScenes();
        }

        private void OnHierarchyChange()
        {
            UpdateScenes();
        }

        private static void OpenScene(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
            UpdateScenes();
        }

        private static void PingSceneInProject(string scenePath)
        {
            var sceneObject = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            EditorGUIUtility.PingObject(sceneObject);
        }

        private static void UpdateScenes()
        {
            _unitySceneGroups = GetScenes(_unitySceneGroups);
        }

        private static List<UnitySceneGroup> GetScenes(List<UnitySceneGroup> unitySceneGroups)
        {
            string[] sceneGUIDs = AssetDatabase.FindAssets("t: scene");

            unitySceneGroups.ForEach(group => group.UnityScenes.Clear());

            sceneGUIDs.ToList().ForEach(sceneGUID =>
            {
                var sceneFilePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                var sceneName = Path.GetFileName(sceneFilePath).Replace(".unity", "");
                var isCurrentScene = sceneFilePath == EditorSceneManager.GetActiveScene().path;
                var directory = Path.GetDirectoryName(sceneFilePath);
                var unitySceneGroup = unitySceneGroups.FirstOrDefault(group => group.GroupName == directory);

                if (unitySceneGroup == null)
                {
                    unitySceneGroup = new UnitySceneGroup(directory);
                    unitySceneGroups.Add(unitySceneGroup);
                }

                unitySceneGroup.UnityScenes.Add(new UnityScene
                {
                    DisplayName = sceneName,
                    FullPath = sceneFilePath,
                    IsActive = isCurrentScene,
                    Directory = directory
                });
            });

            unitySceneGroups.RemoveAll(group => !group.UnityScenes.Any());

            return unitySceneGroups;
        }

        private static void CollapseGroups()
        {
            _unitySceneGroups.ForEach(group => group.Foldout = false);
        }

        private static void ExpandGroups()
        {
            _unitySceneGroups.ForEach(group => group.Foldout = true);
        }
    }

    public class UnityScene
    {
        public bool IsActive { get; set; }
        public string DisplayName { get; set; }
        public string FullPath { get; set; }
        public string Directory { get; set; }
    }

    public class UnitySceneGroup
    {
        public string GroupName { get; private set; }
        public bool Foldout { get; set; } = true;
        public List<UnityScene> UnityScenes { get; set; } = new List<UnityScene>();

        public UnitySceneGroup(string groupName)
        {
            GroupName = groupName;
        }
    }
}
