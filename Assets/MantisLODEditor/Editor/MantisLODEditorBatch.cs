/*--------------------------------------------------------
   MantisLODEditorBatch.cs

   Created by MINGFEN WANG on 15-12-26.
   Copyright (c) 2013 MINGFEN WANG. All rights reserved.
   http://www.mesh-online.net/
   --------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace MantisLODEditor
{
    public class MantisLODEditorBatch : EditorWindow
    {
        private Mantis_Mesh[] Mantis_Meshes = null;

        // define how to optimize the meshes
        private int max_lod_count = 8;
        private bool protect_boundary = true;
        private bool protect_detail = false;
        private bool protect_symmetry = false;
        private bool protect_normal = false;
        private bool protect_shape = true;
        private bool use_detail_map = false;
        private int detail_boost = 10;

        private int state = 0;
        private float start_time = 0.0f;
        private List<string> file_name_list = null;
        private int file_name_index = 0;
        private string file_name_hint = null;
        private string message_hint = null;

        [MenuItem("Window/Mantis LOD Editor/Assets/Create/Mantis LOD Editor Batch")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            MantisLODEditorBatch window = (MantisLODEditorBatch)EditorWindow.GetWindow(typeof(MantisLODEditorBatch));
            window.Show();
        }

        void OnGUI()
        {
            GUIStyle helpStyle = new GUIStyle(GUI.skin.box);
            helpStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

            // display the title
            GUI.enabled = true;
            GUILayout.Label(
                "Mantis LOD Editor Batch Edition v8.1"
                , helpStyle
                , GUILayout.ExpandWidth(true));

            // display controls
            GUI.enabled = (state == 0);
            max_lod_count = EditorGUILayout.IntField("Max LOD Count", max_lod_count);
            protect_boundary = EditorGUILayout.Toggle("Protect Boundary", protect_boundary);
            protect_detail = EditorGUILayout.Toggle("More Details", protect_detail);
            protect_symmetry = EditorGUILayout.Toggle("Protect Symmetry", protect_symmetry);
            protect_normal = EditorGUILayout.Toggle("Protect Hard Edge", protect_normal);
            protect_shape = EditorGUILayout.Toggle("Beautiful Triangles", protect_shape);
            use_detail_map = EditorGUILayout.Toggle("Use Detail Map", use_detail_map);
            if (use_detail_map)
            {
                detail_boost = EditorGUILayout.IntField("Detail Boost", detail_boost);
            }

            // generate progressive meshes
            if (GUILayout.Button("Batch Generate", GUILayout.ExpandWidth(true), GUILayout.ExpandWidth(true)))
            {
                if (GetFileList())
                {
                    message_hint = null;
                    // start the batch
                    start_update(1);
                }
            }

            // we are still alive
            if (state != 0)
            {
                EditorGUILayout.LabelField(file_name_hint);
                EditorGUILayout.LabelField("Time Elapse", (Time.realtimeSinceStartup - start_time).ToString("#0.0"));
            }

            // display something
            if (message_hint != null)
            {
                Debug.Log(message_hint);
                EditorUtility.DisplayDialog("Message", message_hint, "OK");
                message_hint = null;
                /*				GUILayout.Label(
                                                        message_hint
                                                        , helpStyle
                                                        , GUILayout.ExpandWidth(true));*/
            }
        }
        private void start_update(int one_state)
        {
            state = one_state;
            start_time = Time.realtimeSinceStartup;
            EditorApplication.update += Update;
        }
        private void end_update()
        {
            state = 0;
            start_time = 0.0f;
            EditorApplication.update -= Update;
        }
        void Update()
        {
            // we cannot use nice yield method in Editor class, I have to write ugly code:(
            switch (state)
            {
                case 1: ShowModelName(); break;
                case 2: GenerateProgressiveMesh(); break;
                case 3: ShowResult(); break;
            }
            // show time elapse label in main thread
            Repaint();
        }
        private void ShowModelName()
        {
            file_name_hint = file_name_list[file_name_index];
            state = 2;
        }
        private void ShowResult()
        {
            message_hint = null;
            end_update();
        }
        private void GenerateProgressiveMesh()
        {
            try
            {
                CreateAsset(Path.GetFileNameWithoutExtension(file_name_list[file_name_index]));
            }
            finally
            {
            }
            // forward to the next
            file_name_index++;
            // all done
            if (file_name_index == file_name_list.Count)
            {
                state = 3;
            }
            else
            {
                state = 1;
            }
        }
        private bool GetFileList()
        {
            if (!Directory.Exists("Assets/Resources"))
            {
                message_hint = "Please create 'Assets/Resources' directory, then put all your 3d models into the directory and try again.";
                return false;
            }
            // Reset file name list and its index
            file_name_list = new List<string>();
            file_name_index = 0;

            // Get file name list of supported 3d models
            List<string> temp_file_name_list = new List<string>(Directory.GetFiles("Assets/Resources"));
            foreach (string filename in temp_file_name_list)
            {
                string extensionname = Path.GetExtension(filename).ToLower();
                // You may add extensions here
                if (string.Compare(extensionname, ".fbx") == 0 ||
                    string.Compare(extensionname, ".dae") == 0 ||
                    string.Compare(extensionname, ".3ds") == 0 ||
                    string.Compare(extensionname, ".dxf") == 0 ||
                    string.Compare(extensionname, ".obj") == 0 ||
                    string.Compare(extensionname, ".mb") == 0 ||
                    string.Compare(extensionname, ".ma") == 0 ||
                    string.Compare(extensionname, ".c4d") == 0 ||
                    string.Compare(extensionname, ".max") == 0 ||
                    string.Compare(extensionname, ".jas") == 0 ||
                    string.Compare(extensionname, ".lxo") == 0 ||
                    string.Compare(extensionname, ".lws") == 0 ||
                    string.Compare(extensionname, ".blend") == 0)
                {
                    // Add to the list
                    file_name_list.Add(filename);
                }
            }
            // No 3d model in the directory
            if (file_name_list.Count == 0)
            {
                message_hint = "Please put all your 3d models into 'Assets/Resources' and try again.\n\nSupported file extensions:\n.fbx\n.dae\n.3ds\n.dxf\n.obj\n.mb\n.ma\n.c4d\n.max\n.jas\n.lxo\n.lws\n.blend\n\nYou may modify the script(MantisLODEditorPro/Editor/ProgressiveMeshBatch.cs) to support future extensions.";
                return false;
            }
            return true;
        }
        public void CreateAsset(string filename)
        {
            init_all(filename);
            optimize();
            ProgressiveMesh pm = MantisLODEditorUtility.MakeProgressiveMesh(Mantis_Meshes, max_lod_count);
            clean_all();

            string filePath = "Assets/Resources/" + filename + "_Progressive_Mesh.asset";
            MantisLODEditorUtility.SaveProgressiveMesh(pm, filePath);
        }
        private void optimize()
        {
            if (Mantis_Meshes != null)
            {
                MantisLODEditorUtility.Simplify(Mantis_Meshes, protect_boundary, protect_detail, protect_symmetry, protect_normal, protect_shape, use_detail_map, detail_boost);
            }
        }
        private void init_all(string filename)
        {
            get_all_meshes(filename);
            if (Mantis_Meshes != null)
            {
                MantisLODEditorUtility.PrepareSimplify(Mantis_Meshes, false);
            }
        }
        private void get_all_meshes(string filename)
        {
            Mesh[] meshes = Resources.LoadAll<Mesh>(filename);
            int mesh_count = meshes.Length;
            if (mesh_count > 0)
            {
                Mantis_Meshes = new Mantis_Mesh[mesh_count];
                int counter = 0;
                foreach (Mesh mesh in meshes)
                {
                    Mantis_Meshes[counter] = new Mantis_Mesh();
                    Mantis_Meshes[counter].mesh = mesh;
                    counter++;
                }
            }
        }
        private void clean_all()
        {
            if (Mantis_Meshes != null)
            {
                MantisLODEditorUtility.FinishSimplify(Mantis_Meshes, false, true);
            }
        }
    }
}
