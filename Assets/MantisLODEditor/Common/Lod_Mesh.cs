/*--------------------------------------------------------
   Lod_Mesh.cs

   Created by MINGFEN WANG on 13-12-26.
   Copyright (c) 2013 MINGFEN WANG. All rights reserved.
   http://www.mesh-online.net/
   --------------------------------------------------------*/
using UnityEngine;
using System.Collections.Generic;

namespace MantisLODEditor
{
    public class Lod_Mesh
    {
        public Mesh mesh;
        public int triangle_count;
    }
    public class Lod_Mesh_Table
    {
        public Mesh origin_mesh;
        public List<Component> containers;
        public Lod_Mesh[] lod_meshes;
    }
}
