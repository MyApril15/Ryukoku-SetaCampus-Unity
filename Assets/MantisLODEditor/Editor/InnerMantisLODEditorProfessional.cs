/*--------------------------------------------------------
   InnerMantisLODEditorProfessional.cs

   Created by MINGFEN WANG on 13-12-26.
   Copyright (c) 2013 MINGFEN WANG. All rights reserved.
   http://www.mesh-online.net/
   --------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
/*
 * If you want to use the managed plugin, you must use this namespace.
 */
using MantisLOD;
using UnityEditor;
using UnityEngine;

namespace MantisLODEditor
{
    [CustomEditor(typeof(MantisLODEditorProfessional))]
    public class InnerMantisLODEditorProfessional : Editor
    {
        private int max_lod_count = 8;
        private int origin_face_number = 0;
        private int face_number = 0;
        private float quality = 100.0f;
        private float save_quality = 100.0f;
        private bool protect_boundary = true;
        private bool save_protect_boundary = true;
        private bool protect_detail = false;
        private bool save_protect_detail = false;
        private bool protect_symmetry = false;
        private bool save_protect_symmetry = false;
        private bool protect_normal = false;
        private bool save_protect_normal = false;
        private bool protect_shape = true;
        private bool save_protect_shape = true;
        private bool use_detail_map = false;
        private bool save_use_detail_map = false;
        private int detail_boost = 10;
        private int save_detail_boost = 10;
        private Mantis_Mesh[] Mantis_Meshes = null;
        private bool show_title = true;
        private bool show_help = true;
        private bool save_show_help = true;
        private bool optimized = false;

        override public void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (target)
            {
                if (Mantis_Meshes != null)
                {
                    show_title = EditorGUILayout.Foldout(show_title, "Mantis LOD Editor - Professional Edition v8.1");
                    if (show_title)
                    {
                        // A decent style.  Light grey text inside a border.
                        GUIStyle helpStyle = new GUIStyle(GUI.skin.box);
                        helpStyle.wordWrap = true;
                        helpStyle.alignment = TextAnchor.UpperLeft;
                        helpStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                        show_help = EditorGUILayout.Foldout(show_help, show_help ? "Hide Help" : "Show Help");
                        // save all triangle lists as progressive mesh
                        if (GUILayout.Button("Save Progressive Mesh", GUILayout.ExpandWidth(true), GUILayout.ExpandWidth(true)))
                        {
                            if (!optimized)
                            {
                                optimize();
                                optimized = true;
                            }
                            bool read_write_enabled = true;
                            foreach (Mantis_Mesh child in Mantis_Meshes)
                            {
                                if (!child.mesh.isReadable)
                                {
                                    read_write_enabled = false;
                                    break;
                                }
                            }
                            if (!read_write_enabled)
                            {
                                EditorUtility.DisplayDialog("Warning", "Read/Write need to be enabled in the import settings!\n\nPlease select the asset from the 'Project' window, enable the 'Read/Write Enabled' option from the 'Model' tab of the 'Inspector' window, and apply the changes.\n\nProgressive mesh may run well in the editor, but you must enable this option when running on devices!", "OK");
                            }
                            ProgressiveMesh pm = MantisLODEditorUtility.MakeProgressiveMesh(Mantis_Meshes, max_lod_count);
                            string filePath = EditorUtility.SaveFilePanelInProject(
                                "Save Progressive Mesh",
                                ((Component)target).gameObject.name + "_Progressive_Mesh.asset",
                                "asset",
                                "Choose a file location"
                                );
                            if (filePath != "")
                            {
                                MantisLODEditorUtility.SaveProgressiveMesh(pm, filePath);
                            }
                        }
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When Clicked, save the progressive meshes as a single asset file for runtime use."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        EditorGUILayout.Space();
                        // save current mesh as a lod asset file
                        if (GUILayout.Button("Save Current Mesh", GUILayout.ExpandWidth(true), GUILayout.ExpandWidth(true)))
                        {
                            if (!optimized)
                            {
                                optimize();
                                optimized = true;
                            }
                            foreach (Mantis_Mesh child in Mantis_Meshes)
                            {
                                // clone the mesh
                                Mesh new_mesh = (Mesh)Instantiate(child.mesh);
                                MeshUtility.Optimize(new_mesh);
                                string filePath = EditorUtility.SaveFilePanelInProject(
                                    "Save Current Mesh",
                                    new_mesh.name + "_Quality_" + quality.ToString() + ".asset",
                                    "asset",
                                    "Choose a file location"
                                    );
                                if (filePath != "")
                                {
                                    MantisLODEditorUtility.SaveSimplifiedMesh(new_mesh, filePath);
                                }
                            }
                        }
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When Clicked, save the meshes of current quality as LOD asset files."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        max_lod_count = EditorGUILayout.IntField("Max LOD Count", max_lod_count);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "The maximum LOD count we want to generate."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        protect_boundary = EditorGUILayout.Toggle("Protect Boundary", protect_boundary);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, all open boundaries will be protected; Otherwise, some smooth parts of open boundaries will be smartly merged. Both way, uv boundaries and material boundaries will be protected."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        protect_detail = EditorGUILayout.Toggle("More Details", protect_detail);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, more details will be preserved, toggle it only if when making the highest LOD, otherwise, please leave it unchecked to get best results."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        protect_symmetry = EditorGUILayout.Toggle("Protect Symmetry", protect_symmetry);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, all symmetric uv mapping will be preserved, you should check it only if you are making the higher LODs; Otherwise, please leave it unchecked to get best results."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        protect_normal = EditorGUILayout.Toggle("Protect Hard Edge", protect_normal);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, all hard edges will be preserved; If you want to get maximum decimation, please leave it unchecked."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        protect_shape = EditorGUILayout.Toggle("Beautiful Triangles", protect_shape);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, it generates beautiful triangles, but the result may not be better than the tradition method, if this happens, please leave it unchecked."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        use_detail_map = EditorGUILayout.Toggle("Use Detail Map", use_detail_map);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "When checked, it uses the red channel of vertex colors as detail map."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        if (use_detail_map)
                        {
                            detail_boost = EditorGUILayout.IntField("Detail Boost", detail_boost);
                            if (show_help)
                            {
                                GUILayout.Label(
                                    "Boost of detail, more bigger the value, more details will be preserved."
                                    , helpStyle
                                    , GUILayout.ExpandWidth(true));
                            }
                        }
                        EditorGUILayout.LabelField("Triangles", face_number.ToString() + "/" + origin_face_number.ToString());
                        if (show_help)
                        {
                            GUILayout.Label(
                                "Display current triangle number and total triangle number."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        quality = EditorGUILayout.Slider("Quality", quality, 0.0f, 100.0f);
                        if (show_help)
                        {
                            GUILayout.Label(
                                "Drag to change the quality, the mesh will change in real time."
                                , helpStyle
                                , GUILayout.ExpandWidth(true));
                        }
                        if (GUI.changed)
                        {
                            if (save_show_help != show_help)
                            {
                                string filename = "mantis_not_show_help";
                                if (show_help)
                                {
                                    if (System.IO.File.Exists(filename)) System.IO.File.Delete(filename);
                                }
                                else
                                {
                                    if (!System.IO.File.Exists(filename)) System.IO.File.Create(filename);
                                }
                                save_show_help = show_help;
                            }
                            if (save_protect_boundary != protect_boundary)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_protect_boundary = protect_boundary;
                            }
                            if (save_protect_detail != protect_detail)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_protect_detail = protect_detail;
                            }
                            if (save_protect_symmetry != protect_symmetry)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_protect_symmetry = protect_symmetry;
                            }
                            if (save_protect_normal != protect_normal)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_protect_normal = protect_normal;
                            }
                            if (save_protect_shape != protect_shape)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_protect_shape = protect_shape;
                            }
                            if (save_use_detail_map != use_detail_map)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_use_detail_map = use_detail_map;
                            }
                            if (save_detail_boost != detail_boost)
                            {
                                quality = 100.0f;
                                // delete old progressive mesh
                                clean_all();
                                init_all();
                                // create new progressive mesh
                                optimize();
                                optimized = true;
                                save_detail_boost = detail_boost;
                            }
                            if (save_quality != quality)
                            {
                                if (!optimized)
                                {
                                    optimize();
                                    optimized = true;
                                }
                                face_number = MantisLODEditorUtility.SetQuality(Mantis_Meshes, quality);
                                EditorUtility.SetDirty(target);
                                save_quality = quality;
                            }
                        }
                    }
                }
            }
        }
        private void get_all_meshes()
        {
            Component[] allFilters = (Component[])((Component)target).gameObject.GetComponentsInChildren(typeof(MeshFilter));
            Component[] allRenderers = (Component[])((Component)target).gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer));
            int mesh_count = allFilters.Length + allRenderers.Length;
            if (mesh_count > 0)
            {
                Mantis_Meshes = new Mantis_Mesh[mesh_count];
                int counter = 0;
                foreach (Component child in allFilters)
                {
                    Mantis_Meshes[counter] = new Mantis_Mesh();
                    Mantis_Meshes[counter].mesh = ((MeshFilter)child).sharedMesh;
                    counter++;
                }
                foreach (Component child in allRenderers)
                {
                    Mantis_Meshes[counter] = new Mantis_Mesh();
                    Mantis_Meshes[counter].mesh = ((SkinnedMeshRenderer)child).sharedMesh;
                    counter++;
                }
            }
        }
        void Awake()
        {
            init_all();
        }
        private void init_all()
        {
            if (Mantis_Meshes == null)
            {
                if (target)
                {
                    get_all_meshes();
                    if (Mantis_Meshes != null)
                    {
                        face_number = MantisLODEditorUtility.PrepareSimplify(Mantis_Meshes);
                        origin_face_number = face_number;
                        string filename2 = "mantis_not_show_help";
                        if (System.IO.File.Exists(filename2))
                        {
                            show_help = false;
                            save_show_help = false;
                        }
                        else
                        {
                            show_help = true;
                            save_show_help = true;
                        }
                    }
                }
            }
        }
        private void optimize()
        {
            if (target)
            {
                if (Mantis_Meshes != null)
                {
                    MantisLODEditorUtility.Simplify(Mantis_Meshes, protect_boundary, protect_detail, protect_symmetry, protect_normal, protect_shape, use_detail_map, detail_boost);
                }
            }
        }
        private void clean_all()
        {
            // restore triangle list
            if (Mantis_Meshes != null)
            {
                if (target)
                {
                    MantisLODEditorUtility.FinishSimplify(Mantis_Meshes);
                }
            }
        }
        public void OnDisable()
        {
            clean_all();
        }
        public void OnDestroy()
        {
            clean_all();
        }
    }
}
