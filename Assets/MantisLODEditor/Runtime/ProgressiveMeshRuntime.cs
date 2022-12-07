/*--------------------------------------------------------
   ProgressiveMeshRuntime.cs

   Created by MINGFEN WANG on 13-12-26.
   Copyright (c) 2013 MINGFEN WANG. All rights reserved.
   http://www.mesh-online.net/
   --------------------------------------------------------*/
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;

namespace MantisLODEditor
{
    public class ProgressiveMeshRuntime : MonoBehaviour
    {
        // Drag a reference or assign it with code
        public ProgressiveMesh progressiveMesh = null;

        // Optional fields
        public Text fpsHint = null;
        public Text lodHint = null;
        public Text triangleHint = null;

        [HideInInspector]
        public bool optimize_on_the_fly = true;
        // Clamp lod to [minLod, maxLod]
        [HideInInspector]
        public int[] mesh_lod_range = null;
        [HideInInspector]
        public bool never_cull = true;
        [HideInInspector]
        public int lod_strategy = 1;
        [HideInInspector]
        public float cull_ratio = 0.1f;
        [HideInInspector]
        public float disappear_distance = 250.0f;
        [HideInInspector]
        public float updateInterval = 0.25f;

        private int current_lod = -1;

        private Component[] allBasicRenderers = null;

        // How often to check lod changes, default four times per second.
        // You may increase the value to balance the load if you have hundreds of 3d models in the scene.
        private float currentTimeToInterval = 0.0f;
        private bool culled = false;
        private bool working = false;

#if UNITY_EDITOR
        [MenuItem("Window/Mantis LOD Editor/Component/Runtime/Progressive Mesh Runtime")]
        public static void AddComponent()
        {
            GameObject SelectedObject = Selection.activeGameObject;
            if (SelectedObject)
            {
                // Register root object for undo.
                Undo.RegisterCreatedObjectUndo(SelectedObject.AddComponent(typeof(ProgressiveMeshRuntime)), "Add Progressive Mesh Runtime");
            }
        }
        [MenuItem("Window/Mantis LOD Editor/Component/Runtime/Progressive Mesh Runtime", true)]
        static bool ValidateAddComponent()
        {
            // Return false if no gameobject is selected.
            return Selection.activeGameObject != null;
        }
#endif
        void Awake()
        {
            get_all_meshes();
        }
        // Use this for initialization
        void Start()
        {
        }
        private float ratio_of_screen()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Component child in allBasicRenderers)
            {
                Renderer rend = (Renderer)child;
                Vector3 center = rend.bounds.center;
                float radius = rend.bounds.extents.magnitude;
                Vector3[] six_points = new Vector3[6];
                six_points[0] = Camera.main.WorldToScreenPoint(new Vector3(center.x - radius, center.y, center.z));
                six_points[1] = Camera.main.WorldToScreenPoint(new Vector3(center.x + radius, center.y, center.z));
                six_points[2] = Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y - radius, center.z));
                six_points[3] = Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y + radius, center.z));
                six_points[4] = Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y, center.z - radius));
                six_points[5] = Camera.main.WorldToScreenPoint(new Vector3(center.x, center.y, center.z + radius));
                foreach (Vector3 v in six_points)
                {
                    if (v.x < min.x) min.x = v.x;
                    if (v.y < min.y) min.y = v.y;
                    if (v.x > max.x) max.x = v.x;
                    if (v.y > max.y) max.y = v.y;
                }
            }
            float ratio_width = (max.x - min.x) / Camera.main.pixelWidth;
            float ratio_height = (max.y - min.y) / Camera.main.pixelHeight;
            float ratio = (ratio_width > ratio_height) ? ratio_width : ratio_height;
            if (ratio > 1.0f) ratio = 1.0f;

            return ratio;
        }
        private float ratio_of_distance(float distance0)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Component child in allBasicRenderers)
            {
                Renderer rend = (Renderer)child;
                Vector3 center = rend.bounds.center;
                float radius = rend.bounds.extents.magnitude;
                Vector3[] six_points = new Vector3[6];
                six_points[0] = new Vector3(center.x - radius, center.y, center.z);
                six_points[1] = new Vector3(center.x + radius, center.y, center.z);
                six_points[2] = new Vector3(center.x, center.y - radius, center.z);
                six_points[3] = new Vector3(center.x, center.y + radius, center.z);
                six_points[4] = new Vector3(center.x, center.y, center.z - radius);
                six_points[5] = new Vector3(center.x, center.y, center.z + radius);
                foreach (Vector3 v in six_points)
                {
                    if (v.x < min.x) min.x = v.x;
                    if (v.y < min.y) min.y = v.y;
                    if (v.z < min.z) min.z = v.z;
                    if (v.x > max.x) max.x = v.x;
                    if (v.y > max.y) max.y = v.y;
                    if (v.z > max.z) max.z = v.z;
                }
            }
            Vector3 average_position = (min + max) * 0.5f;
            float distance = Vector3.Distance(Camera.main.transform.position, average_position);
            float ratio = 1.0f - distance / distance0;
            if (ratio < 0.0f) ratio = 0.0f;

            return ratio;
        }
        // Update is called once per frame
        void Update()
        {
            if (progressiveMesh)
            {
                currentTimeToInterval -= Time.deltaTime;
                // time out
                if (currentTimeToInterval <= 0.0f)
                {
                    // detect if the game object is visible
                    bool visable = false;
                    if (!culled)
                    {
                        allBasicRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(Renderer)));
                        foreach (Component child in allBasicRenderers)
                        {
                            if (((Renderer)child).isVisible) visable = true;
                            break;
                        }
                    }
                    // we only change levels when the game object had been culled by ourselves or is visable
                    if (culled || visable)
                    {
                        float ratio = 0.0f;
                        // we only calculate ratio of screen when the main camera is active in hierarchy
                        if (Camera.main != null && Camera.main.gameObject != null && Camera.main.gameObject.activeInHierarchy)
                        {
                            allBasicRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(Renderer)));
                            if (lod_strategy == 0) ratio = ratio_of_screen();
                            if (lod_strategy == 1) ratio = ratio_of_distance(disappear_distance);
                        }
                        // you may change cull condition here
                        if (never_cull == false && ratio < cull_ratio)
                        {
                            // cull the game object
                            if (!culled)
                            {
                                // cull me
                                allBasicRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(Renderer)));
                                foreach (Component child in allBasicRenderers)
                                {
                                    ((Renderer)child).enabled = false;
                                }
                                culled = true;
                            }
                        }
                        else
                        {
                            // show the game object
                            if (culled)
                            {
                                // show me
                                allBasicRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(Renderer)));
                                foreach (Component child in allBasicRenderers)
                                {
                                    ((Renderer)child).enabled = true;
                                }
                                culled = false;
                            }
                            // get lod count
                            int max_lod_count = progressiveMesh.triangles[0];
                            // set triangle list according to current lod
                            int lod = (int)((1.0f - ratio) * max_lod_count);
                            // clamp the value
                            if (lod > max_lod_count - 1) lod = max_lod_count - 1;
                            // lod changed
                            if (current_lod != lod)
                            {
                                Component[] MeshFilters = (Component[])(gameObject.GetComponentsInChildren(typeof(MeshFilter)));
                                Component[] SkinnedMeshRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer)));
                                Component[] Components = new Component[MeshFilters.Length + SkinnedMeshRenderers.Length];
                                Array.Copy(MeshFilters, 0, Components, 0, MeshFilters.Length);
                                Array.Copy(SkinnedMeshRenderers, 0, Components, MeshFilters.Length, SkinnedMeshRenderers.Length);
                                int total_triangles_count = MantisLODEditorUtility.SwitchRuntimeLOD(progressiveMesh, mesh_lod_range, lod, Components);
                                // update read only status
                                if (lodHint) lodHint.text = "Level Of Detail: " + lod.ToString();
                                if (triangleHint) triangleHint.text = "Triangle Count: " + (total_triangles_count / 3).ToString();
                                current_lod = lod;
                            }
                        }
                    }
                    if (fpsHint)
                    {
                        int fps = Mathf.RoundToInt(1.0f / Time.smoothDeltaTime);
                        fpsHint.text = "FPS: " + fps.ToString();
                    }
                    //reset timer
                    currentTimeToInterval = updateInterval + (UnityEngine.Random.value + 0.5f) * currentTimeToInterval;
                }
            }
        }
        private void create_default_mesh_lod_range()
        {
            int max_lod_count = progressiveMesh.triangles[0];
            int mesh_count = progressiveMesh.triangles[1];
            mesh_lod_range = new int[mesh_count * 2];
            for (int i = 0; i < mesh_count; i++)
            {
                mesh_lod_range[i * 2] = 0;
                mesh_lod_range[i * 2 + 1] = max_lod_count - 1;
            }
        }
        private void get_all_meshes()
        {
            if (!working)
            {
                int max_lod_count = progressiveMesh.triangles[0];
                if (mesh_lod_range == null || mesh_lod_range.Length == 0)
                {
                    create_default_mesh_lod_range();
                }
                Component[] MeshFilters = (Component[])(gameObject.GetComponentsInChildren(typeof(MeshFilter)));
                Component[] SkinnedMeshRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer)));
                Component[] Components = new Component[MeshFilters.Length + SkinnedMeshRenderers.Length];
                Array.Copy(MeshFilters, 0, Components, 0, MeshFilters.Length);
                Array.Copy(SkinnedMeshRenderers, 0, Components, MeshFilters.Length, SkinnedMeshRenderers.Length);
                MantisLODEditorUtility.GenerateRuntimeLOD(progressiveMesh, Components, optimize_on_the_fly);

                // get all renderers
                allBasicRenderers = (Component[])(gameObject.GetComponentsInChildren(typeof(Renderer)));
                // We use random value to spread the update moment in range [0, updateInterval]
                currentTimeToInterval = UnityEngine.Random.value * updateInterval;

                // init current lod
                current_lod = -1;

                working = true;
            }
        }
        public void reset_all_parameters()
        {
            optimize_on_the_fly = true;
            mesh_lod_range = null;
            never_cull = true;
            lod_strategy = 1;
            cull_ratio = 0.1f;
            disappear_distance = 250.0f;
            updateInterval = 0.25f;
        }
        private void clean_all()
        {
            if (working)
            {
                MantisLODEditorUtility.FinishRuntimeLOD(progressiveMesh);
                allBasicRenderers = null;

                working = false;
            }
        }
        void OnEnable()
        {
            Awake();
            Start();
        }
        void OnDisable()
        {
            clean_all();
        }
        void OnDestroy()
        {
            clean_all();
        }
    }
}
