/*--------------------------------------------------------
   MantisLODEditorUtility.cs

   Created by MINGFEN WANG on 13-12-26.
   Copyright (c) 2013 MINGFEN WANG. All rights reserved.
   http://www.mesh-online.net/
   --------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MantisLODEditor
{
#if !UNITY_EDITOR
    using static MantisLOD.MantisLODSimpler;
#endif
    public static class MantisLODEditorUtility
    {
#if UNITY_EDITOR
        [DllImport("MantisLOD")]
        private static extern int create_progressive_mesh(Vector3[] vertex_array, int vertex_count, int[] triangle_array, int triangle_count, Vector3[] normal_array, int normal_count, Color[] color_array, int color_count, Vector2[] uv_array, int uv_count, int protect_boundary, int protect_detail, int protect_symmetry, int protect_normal, int protect_shape, int use_detail_map, int detail_boost);
        [DllImport("MantisLOD")]
        private static extern int get_triangle_list(int index, float goal, int[] triangles, ref int triangle_count);
        [DllImport("MantisLOD")]
        private static extern int delete_progressive_mesh(int index);
#endif
        public static string get_uuid_from_mesh(Mesh mesh)
        {
            string uuid = mesh.name + "_" + mesh.vertexCount.ToString() + "_" + mesh.subMeshCount.ToString();
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                uuid += "_" + mesh.GetIndexCount(i).ToString();
            }
            return uuid;
        }
        public static int PrepareSimplify(Mantis_Mesh[] Mantis_Meshes, bool use_origin_triangles = true)
        {
            int face_number = 0;
            foreach (Mantis_Mesh child in Mantis_Meshes)
            {
                int triangle_number = child.mesh.triangles.Length;
                // out data is large than origin data
                child.out_triangles = new int[triangle_number + child.mesh.subMeshCount];
                if (use_origin_triangles)
                {
                    child.origin_triangles = new int[child.mesh.subMeshCount][];
                    for (int i = 0; i < child.mesh.subMeshCount; i++)
                    {
                        int[] sub_triangles = child.mesh.GetTriangles(i);
                        face_number += sub_triangles.Length / 3;
                        // save origin triangle list
                        child.origin_triangles[i] = new int[sub_triangles.Length];
                        Array.Copy(sub_triangles, child.origin_triangles[i], sub_triangles.Length);
                    }
                }
                child.index = -1;
                child.uuid = get_uuid_from_mesh(child.mesh);
            }
            return face_number;
        }
        public static void Simplify(Mantis_Mesh[] Mantis_Meshes, bool protect_boundary, bool protect_detail, bool protect_symmetry, bool protect_normal, bool protect_shape, bool use_detail_map, int detail_boost)
        {
            foreach (Mantis_Mesh child in Mantis_Meshes)
            {
                int triangle_number = child.mesh.triangles.Length;
                Vector3[] vertices = child.mesh.vertices;
                // flat input array is larger than the original triangles size
                int[] triangles = new int[triangle_number + child.mesh.subMeshCount];
                // we need normal data to protect normal boundary
                Vector3[] normals = child.mesh.normals;
                // we need color data to protect color boundary
                Color[] colors = child.mesh.colors;
                // we need uv data to protect uv boundary
                Vector2[] uvs = child.mesh.uv;
                // concatenate all data together
                int counter = 0;
                for (int i = 0; i < child.mesh.subMeshCount; i++)
                {
                    int[] sub_triangles = child.mesh.GetTriangles(i);
                    triangles[counter] = sub_triangles.Length;
                    counter++;
                    Array.Copy(sub_triangles, 0, triangles, counter, sub_triangles.Length);
                    counter += sub_triangles.Length;
                }
                // create progressive mesh
                child.index = create_progressive_mesh(vertices, vertices.Length, triangles, counter, normals, normals.Length, colors, colors.Length, uvs, uvs.Length, protect_boundary ? 1 : 0, protect_detail ? 1 : 0, protect_symmetry ? 1 : 0, protect_normal ? 1 : 0, protect_shape ? 1 : 0, use_detail_map ? 1 : 0, detail_boost);
            }
        }
        public static int SetQuality(Mantis_Mesh[] Mantis_Meshes, float quality)
        {
            int face_number = 0;
            foreach (Mantis_Mesh child in Mantis_Meshes)
            {
                // get triangle list by quality value
                if (child.index != -1 && get_triangle_list(child.index, quality, child.out_triangles, ref child.out_count) == 1)
                {
                    if (child.out_count > 0)
                    {
                        int counter = 0;
                        int mat = 0;
                        while (counter < child.out_count)
                        {
                            int len = child.out_triangles[counter];
                            counter++;
                            if (len > 0)
                            {
                                int[] new_triangles = new int[len];
                                Array.Copy(child.out_triangles, counter, new_triangles, 0, len);
                                child.mesh.SetTriangles(new_triangles, mat);
                                counter += len;
                            }
                            else
                            {
                                child.mesh.SetTriangles((int[])null, mat);
                            }
                            mat++;
                        }
                        face_number += child.mesh.triangles.Length / 3;
                    }
                }
            }
            return face_number;
        }
        public static void SaveSimplifiedMesh(Mesh mesh, string filePath)
        {
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mesh, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        public static void FinishSimplify(Mantis_Mesh[] Mantis_Meshes, bool use_origin_triangles = true, bool unload_asset = false)
        {
            if (Mantis_Meshes != null)
            {
                foreach (Mantis_Mesh child in Mantis_Meshes)
                {
                    if (child.index != -1)
                    {
                        if (use_origin_triangles)
                        {
                            // restore triangle list
                            for (int i = 0; i < child.mesh.subMeshCount; i++)
                            {
                                child.mesh.SetTriangles(child.origin_triangles[i], i);
                            }
                        }
                        if (unload_asset)
                        {
                            Resources.UnloadAsset(child.mesh);
                        }
                        delete_progressive_mesh(child.index);
                        child.index = -1;
                    }
                }
                Mantis_Meshes = null;
            }
        }
        public static ProgressiveMesh MakeProgressiveMesh(Mantis_Mesh[] Mantis_Meshes, int max_lod_count)
        {
            ProgressiveMesh pm = (ProgressiveMesh)ScriptableObject.CreateInstance(typeof(ProgressiveMesh));
            int triangle_count = 0;
            int[][][][] temp_triangles;
            temp_triangles = new int[max_lod_count][][][];
            // max lod count
            triangle_count++;
            for (int lod = 0; lod < temp_triangles.Length; lod++)
            {
                float quality = 100.0f * (temp_triangles.Length - lod) / temp_triangles.Length;
                temp_triangles[lod] = new int[Mantis_Meshes.Length][][];
                // mesh count
                triangle_count++;
                int mesh_count = 0;
                pm.uuids = new string[Mantis_Meshes.Length];
                foreach (Mantis_Mesh child in Mantis_Meshes)
                {
                    // get triangle list by quality value
                    if (child.index != -1 && get_triangle_list(child.index, quality, child.out_triangles, ref child.out_count) == 1)
                    {
                        if (child.out_count > 0)
                        {
                            int counter = 0;
                            int mat = 0;
                            temp_triangles[lod][mesh_count] = new int[child.mesh.subMeshCount][];
                            // sub mesh count
                            triangle_count++;
                            while (counter < child.out_count)
                            {
                                int len = child.out_triangles[counter];
                                // triangle count
                                triangle_count++;
                                // triangle list count
                                triangle_count += len;
                                counter++;
                                int[] new_triangles = new int[len];
                                Array.Copy(child.out_triangles, counter, new_triangles, 0, len);
                                temp_triangles[lod][mesh_count][mat] = new_triangles;
                                counter += len;
                                mat++;
                            }
                        }
                        else
                        {
                            temp_triangles[lod][mesh_count] = new int[child.mesh.subMeshCount][];
                            // sub mesh count
                            triangle_count++;
                            for (int mat = 0; mat < temp_triangles[lod][mesh_count].Length; mat++)
                            {
                                temp_triangles[lod][mesh_count][mat] = new int[0];
                                // triangle count
                                triangle_count++;
                            }
                        }
                    }
                    pm.uuids[mesh_count] = child.uuid;
                    mesh_count++;
                }
            }
            // create fix size array
            pm.triangles = new int[triangle_count];

            // reset the counter
            triangle_count = 0;
            // max lod count
            pm.triangles[triangle_count] = temp_triangles.Length;
            triangle_count++;
            for (int lod = 0; lod < temp_triangles.Length; lod++)
            {
                // mesh count
                pm.triangles[triangle_count] = temp_triangles[lod].Length;
                triangle_count++;
                for (int mesh_count = 0; mesh_count < temp_triangles[lod].Length; mesh_count++)
                {
                    // sub mesh count
                    pm.triangles[triangle_count] = temp_triangles[lod][mesh_count].Length;
                    triangle_count++;
                    for (int mat = 0; mat < temp_triangles[lod][mesh_count].Length; mat++)
                    {
                        // triangle count
                        pm.triangles[triangle_count] = temp_triangles[lod][mesh_count][mat].Length;
                        triangle_count++;
                        Array.Copy(temp_triangles[lod][mesh_count][mat], 0, pm.triangles, triangle_count, temp_triangles[lod][mesh_count][mat].Length);
                        // triangle list count
                        triangle_count += temp_triangles[lod][mesh_count][mat].Length;
                    }
                }
            }
            return pm;
        }
        public static void SaveProgressiveMesh(ProgressiveMesh pm, string filePath)
        {
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(pm, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        public static ProgressiveMesh LoadProgressiveMesh(string filePath)
        {
            return Resources.Load<ProgressiveMesh>(filePath);
        }
        public static int get_triangles_count_from_progressive_mesh(ProgressiveMesh pm, int _lod, int _mesh_count)
        {
            int counter = 0;
            int triangle_count = 0;
            // max lod count
            int max_lod_count = pm.triangles[triangle_count];
            triangle_count++;
            for (int lod = 0; lod < max_lod_count; lod++)
            {
                // max mesh count
                int max_mesh_count = pm.triangles[triangle_count];
                triangle_count++;
                for (int mesh_count = 0; mesh_count < max_mesh_count; mesh_count++)
                {
                    // max sub mesh count
                    int max_sub_mesh_count = pm.triangles[triangle_count];
                    triangle_count++;
                    for (int mat = 0; mat < max_sub_mesh_count; mat++)
                    {
                        // max triangle count
                        int max_triangle_count = pm.triangles[triangle_count];
                        triangle_count++;
                        // here it is
                        if (lod == _lod && mesh_count == _mesh_count)
                        {
                            counter += max_triangle_count;
                        }
                        // triangle list count
                        triangle_count += max_triangle_count;
                    }
                }
            }
            return counter / 3;
        }
        private static int[] get_triangles_from_progressive_mesh(ProgressiveMesh pm, int _lod, int _mesh_count, int _mat)
        {
            int triangle_count = 0;
            // max lod count
            int max_lod_count = pm.triangles[triangle_count];
            triangle_count++;
            for (int lod = 0; lod < max_lod_count; lod++)
            {
                // max mesh count
                int max_mesh_count = pm.triangles[triangle_count];
                triangle_count++;
                for (int mesh_count = 0; mesh_count < max_mesh_count; mesh_count++)
                {
                    // max sub mesh count
                    int max_sub_mesh_count = pm.triangles[triangle_count];
                    triangle_count++;
                    for (int mat = 0; mat < max_sub_mesh_count; mat++)
                    {
                        // max triangle count
                        int max_triangle_count = pm.triangles[triangle_count];
                        triangle_count++;
                        // here it is
                        if (lod == _lod && mesh_count == _mesh_count && mat == _mat)
                        {
                            int[] new_triangles = new int[max_triangle_count];
                            Array.Copy(pm.triangles, triangle_count, new_triangles, 0, max_triangle_count);
                            return new_triangles;
                        }
                        // triangle list count
                        triangle_count += max_triangle_count;
                    }
                }
            }
            return null;
        }
        private static void set_triangles(ProgressiveMesh pm, Mesh mesh, string uuid, int lod)
        {
            int mesh_index = Array.IndexOf(pm.uuids, uuid);
            // the mesh is in the progressive mesh list
            if (mesh_index != -1)
            {
                for (int mat = 0; mat < mesh.subMeshCount; mat++)
                {
                    int[] triangle_list = get_triangles_from_progressive_mesh(pm, lod, mesh_index, mat);
                    mesh.SetTriangles(triangle_list, mat);
                }
            }
        }
        public static void GenerateRuntimeLOD(ProgressiveMesh pm, Component[] containers, bool optimize_on_the_fly)
        {
            if (pm == null)
            {
                return;
            }

            // mesh lods dictionary does not exist
            if (pm.lod_meshes_dic == null)
            {
                pm.lod_meshes_dic = new Dictionary<string, Lod_Mesh_Table>();
            }

            int max_lod_count = pm.triangles[0];
            foreach (Component container in containers)
            {
                // get original shared mesh
                Mesh origin_mesh = null;
                if (container is MeshFilter)
                {
                    origin_mesh = ((MeshFilter)container).sharedMesh;
                }
                else if (container is SkinnedMeshRenderer)
                {
                    origin_mesh = ((SkinnedMeshRenderer)container).sharedMesh;
                }

                // no shared mesh
                if (origin_mesh == null)
                {
                    continue;
                }

                // create mesh lods if it does not exist
                string uuid = get_uuid_from_mesh(origin_mesh);
                if (!pm.lod_meshes_dic.ContainsKey(uuid))
                {
                    int mesh_index = Array.IndexOf(pm.uuids, uuid);
                    if (mesh_index != -1)
                    {
                        Lod_Mesh_Table lod_mesh_table = new Lod_Mesh_Table();
                        lod_mesh_table.containers = new List<Component>();
                        lod_mesh_table.containers.Add(container);
                        lod_mesh_table.origin_mesh = origin_mesh;
                        lod_mesh_table.lod_meshes = new Lod_Mesh[max_lod_count];
                        for (int lod = 0; lod < max_lod_count; lod++)
                        {
                            lod_mesh_table.lod_meshes[lod] = new Lod_Mesh();
                            lod_mesh_table.lod_meshes[lod].mesh = UnityEngine.Object.Instantiate(origin_mesh);
                            set_triangles(pm, lod_mesh_table.lod_meshes[lod].mesh, uuid, lod);
                            if (optimize_on_the_fly)
                            {
#if UNITY_EDITOR
                                MeshUtility.Optimize(lod_mesh_table.lod_meshes[lod].mesh);
#elif UNITY_2019_1_OR_NEWER
                                lod_mesh_table.lod_meshes[lod].mesh.Optimize();
#endif
                            }
                            lod_mesh_table.lod_meshes[lod].triangle_count = lod_mesh_table.lod_meshes[lod].mesh.triangles.Length;
                        }
                        pm.lod_meshes_dic.Add(uuid, lod_mesh_table);
                    }
                }
                else
                {
                    pm.lod_meshes_dic[uuid].containers.Add(container);
                }
            }
        }
        public static int SwitchRuntimeLOD(ProgressiveMesh pm, int[] mesh_lod_range, int lod, Component[] Components)
        {
            int total_triangles_count = 0;
            if (pm == null)
            {
                return total_triangles_count;
            }

            if (pm.lod_meshes_dic == null)
            {
                return total_triangles_count;
            }

            foreach (KeyValuePair<string, Lod_Mesh_Table> kv in pm.lod_meshes_dic)
            {
                int mesh_index = Array.IndexOf(pm.uuids, kv.Key);

                // the mesh is not in the progressive mesh list
                if (mesh_index == -1)
                {
                    continue;
                }

                int clamp_lod = lod;
                // clamp to valid range
                if (clamp_lod < mesh_lod_range[mesh_index * 2]) clamp_lod = mesh_lod_range[mesh_index * 2];
                if (clamp_lod > mesh_lod_range[mesh_index * 2 + 1]) clamp_lod = mesh_lod_range[mesh_index * 2 + 1];

                // setup lod for all containers
                foreach (Component container in kv.Value.containers)
                {
                    foreach (Component container2 in Components) {
                        if (container == container2) {
                            if (container is MeshFilter)
                            {
                                ((MeshFilter)container).sharedMesh = kv.Value.lod_meshes[clamp_lod].mesh;
                            }
                            else
                            {
                                ((SkinnedMeshRenderer)container).sharedMesh = kv.Value.lod_meshes[clamp_lod].mesh;
                            }
                            total_triangles_count += kv.Value.lod_meshes[clamp_lod].triangle_count;
                        }
                    }
                }
            }
            return total_triangles_count;
        }
        public static void FinishRuntimeLOD(ProgressiveMesh pm)
        {
            if (pm == null)
            {
                return;
            }

            if (pm.lod_meshes_dic == null)
            {
                return;
            }

            foreach (Lod_Mesh_Table lod_mesh_table in pm.lod_meshes_dic.Values)
            {
                foreach (Component container in lod_mesh_table.containers)
                {
                    if (container is MeshFilter)
                    {
                        ((MeshFilter)container).sharedMesh = lod_mesh_table.origin_mesh;
                    }
                    else
                    {
                        ((SkinnedMeshRenderer)container).sharedMesh = lod_mesh_table.origin_mesh;
                    }
                }
            }
            pm.lod_meshes_dic = null;
        }
    }
}
