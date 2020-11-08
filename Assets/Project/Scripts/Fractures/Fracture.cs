using System.Linq;
using Project.Scripts.Utils;
using UnityEngine;

namespace Project.Scripts.Fractures {
    public class Fracture {
        private int totalChunks;
        private int seed;
        private NvMesh mesh;
        private Material insideMaterial;
        private Material outsideMaterial;
        private float jointBreakForce;
        private float totalMass;

        public Fracture(int totalChunks, int seed, NvMesh mesh, Material insideMaterial, Material outsideMaterial, float jointBreakForce, float totalMass) {
            this.totalChunks = totalChunks;
            this.seed = seed;
            this.mesh = mesh;
            this.insideMaterial = insideMaterial;
            this.outsideMaterial = outsideMaterial;
            this.jointBreakForce = jointBreakForce;
            this.totalMass = totalMass;
        }

        public Fracture(int totalChunks, int seed, Mesh mesh, Material insideMaterial, Material outsideMaterial, float jointBreakForce, float totalMass) {
            this.totalChunks = totalChunks;
            this.seed = seed;
            this.mesh = new NvMesh(
                mesh.vertices,
                mesh.normals,
                mesh.uv,
                mesh.vertexCount,
                mesh.GetIndices(0),
                (int)mesh.GetIndexCount(0)
            );
            this.insideMaterial = insideMaterial;
            this.outsideMaterial = outsideMaterial;
            this.jointBreakForce = jointBreakForce;
            this.totalMass = totalMass;
        }

        public void Bake(GameObject go, int layersLeft, int chunksPerLayer, float internalStrength, float density) {
            NvBlastExtUnity.setSeed(seed);

            var fractureTool = new NvFractureTool();

            fractureTool.setSourceMesh(this.mesh);
            fractureTool.setRemoveIslands(false);
            Voronoi(fractureTool, this.mesh);
            fractureTool.finalizeFracturing();

            for (var i = 1; i < fractureTool.getChunkCount(); i++) {
                GameObject chunk = new GameObject("Chunk" + i);
                chunk.transform.SetParent(go.transform, false);

                Setup(i, chunk, fractureTool);
                FractureUtils.ConnectTouchingChunks(chunk, jointBreakForce);

                if (layersLeft > 0) {
                    NvMesh chunkMesh = fractureTool.getChunkMesh(i, false);
                    Vector3 dimensions = chunkMesh.toUnityMesh().bounds.size;

                    Fracture fracture = new Fracture(
                        chunksPerLayer,
                        seed,
                        chunkMesh,
                        insideMaterial,
                        outsideMaterial,
                        internalStrength,
                        density * (dimensions.x * dimensions.y * dimensions.z)
                    );

                    GameObject fractured = new GameObject();
                    fractured.transform.SetParent(chunk.transform, false);
                    fractured.name = "fracture layer #" + layersLeft;

                    fracture.Bake(fractured, layersLeft - 1, chunksPerLayer, internalStrength, density);
                    fractured.SetActive(false);

                    ChunkGraphManager graphManager = fractured.AddComponent<ChunkGraphManager>();
                    graphManager.Setup(fractured.gameObject.GetComponentsInChildren<Rigidbody>());
                }
            }
        }

        private void Setup(int i, GameObject chunk, NvFractureTool fractureTool) {
            var renderer = chunk.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] {
                outsideMaterial,
                insideMaterial
            };

            var outside = fractureTool.getChunkMesh(i, false);
            var inside = fractureTool.getChunkMesh(i, true);

            var mesh = outside.toUnityMesh();
            mesh.subMeshCount = 2;
            mesh.SetIndices(inside.getIndexes(), MeshTopology.Triangles, 1);

            var meshFilter = chunk.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var rigibody = chunk.AddComponent<Rigidbody>();
            rigibody.mass = totalMass / totalChunks;

            var mc = chunk.AddComponent<MeshCollider>();
            mc.inflateMesh = true;
            mc.convex = true;
        }

        private void Voronoi(NvFractureTool fractureTool, NvMesh mesh) {
            NvVoronoiSitesGenerator sites = new NvVoronoiSitesGenerator(mesh);
            sites.uniformlyGenerateSitesInMesh(totalChunks);
            fractureTool.voronoiFracturing(0, sites);
        }
    }
}