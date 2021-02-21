using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CCMeshData
{
    public List<Vector3> points; // Original mesh points
    public List<Vector4> faces; // Original mesh quad faces
    public List<Vector4> edges; // Original mesh edges
    public List<Vector3> facePoints; // Face points, as described in the Catmull-Clark algorithm
    public List<Vector3> edgePoints; // Edge points, as described in the Catmull-Clark algorithm
    public List<Vector3> newPoints; // New locations of the original mesh points, according to Catmull-Clark
}


public static class CatmullClark
{
    // Returns a QuadMeshData representing the input mesh after one iteration of Catmull-Clark subdivision.
    public static QuadMeshData Subdivide(QuadMeshData quadMeshData)
    {
        // Create and initialize a CCMeshData corresponding to the given QuadMeshData
        CCMeshData meshData = new CCMeshData();
        meshData.points = quadMeshData.vertices;
        meshData.faces = quadMeshData.quads;
        meshData.edges = GetEdges(meshData);
        meshData.facePoints = GetFacePoints(meshData);
        meshData.edgePoints = GetEdgePoints(meshData);
        meshData.newPoints = GetNewPoints(meshData);
        List<List<int>> faceEdges = new List<List<int>>();
        for( int i=0; i< meshData.faces.Count; ++i)
        {
            faceEdges.Add(new List<int>());
        }
        
        for (int i=0; i<meshData.edges.Count; ++i)
        {
            faceEdges[(int)meshData.edges[i][2]].Add(i);
            faceEdges[(int)meshData.edges[i][3]].Add(i);
        }
        List<List<Vector2>> faceEdgePoints = new List<List<Vector2>>();
        for (int i = 0; i < faceEdges.Count; ++i)
        {
            faceEdgePoints.Add(new List<Vector2>());
            for (int j = 0; j < 4; ++j)
            {
                if (i == meshData.edges[faceEdges[i][j]][2])
                {
                    faceEdgePoints[i].Add(new Vector2(meshData.edges[faceEdges[i][j]][0], meshData.edges[faceEdges[i][j]][1]));
                }
                else
                {
                    faceEdgePoints[i].Add(new Vector2(meshData.edges[faceEdges[i][j]][1], meshData.edges[faceEdges[i][j]][0]));
                }
            }
        }
        List<Vector3> newPoints = meshData.facePoints.Concat(meshData.edgePoints).Concat(meshData.newPoints).ToList() ;
        int edgePointStart = meshData.facePoints.Count;
        int newPointStart = edgePointStart + meshData.edgePoints.Count;
        List<Vector4> newFaces = new List<Vector4>();
        for (int i=0; i< faceEdges.Count; ++i)
        {
            for(int j = 0; j<3; ++j)
            {
                for(int k= j+1; k<4; ++k)
                {
                    if(faceEdgePoints[i][j][0] == faceEdgePoints[i][k][1])
                    {
                        newFaces.Add(new Vector4(i, faceEdges[i][k] + edgePointStart, faceEdgePoints[i][j][0] + newPointStart, faceEdges[i][j] + edgePointStart));
                    }
                    else if (faceEdgePoints[i][j][1] == faceEdgePoints[i][k][0])
                    {
                        newFaces.Add(new Vector4(i, faceEdges[i][j] + edgePointStart, faceEdgePoints[i][j][1] + newPointStart, faceEdges[i][k] + edgePointStart));
                    }
                }
            }
        }
        return new QuadMeshData(newPoints, newFaces);
    }
  
    // Returns a list of all edges in the mesh defined by given points and faces.
    // Each edge is represented by Vector4(p1, p2, f1, f2)
    // p1, p2 are the edge vertices
    // f1, f2 are faces incident to the edge. If the edge belongs to one face only, f2 is -1
    public static List<Vector4> GetEdges(CCMeshData mesh)
    {
        List<Vector4> edges = new List<Vector4>();
        Dictionary<Vector3, Vector4> dict = new Dictionary<Vector3, Vector4>();   
        for (int i=0; i<mesh.faces.Count; ++i)
        {
            Vector3 place;
            for (int j=0; j<3; ++j)
            {     
                place = (mesh.points[(int) mesh.faces[i][j]]) + (mesh.points[(int) mesh.faces[i][j+1]]);
                if (dict.ContainsKey(place))
                {
                    dict[place] = new Vector4(dict[place][0], dict[place][1], dict[place][2], i);
                }
                else
                {
                    dict.Add(place, new Vector4(mesh.faces[i][j], mesh.faces[i][j + 1], i, -1));
                }
            }
            place = mesh.points[(int) mesh.faces[i][3]] + mesh.points[(int) mesh.faces[i][0]];
            if (dict.ContainsKey(place))
            {
                dict[place] = new Vector4(dict[place][0], dict[place][1], dict[place][2], i);
            }
            else
            {
                dict.Add(place, new Vector4(mesh.faces[i][3], mesh.faces[i][0], i, -1));
            }
        }
        foreach(Vector4 value in dict.Values)
        {
            edges.Add(value);
        }
        return edges;
    }

    // Returns a list of "face points" for the given CCMeshData, as described in the Catmull-Clark algorithm 
    public static List<Vector3> GetFacePoints(CCMeshData mesh)
    {
        List<Vector3> face_points = new List<Vector3>();
        foreach(Vector4 face in mesh.faces)
        {
            Vector3 new_point = (mesh.points[(int)face[0]] + mesh.points[(int)face[1]] + mesh.points[(int)face[2]] + mesh.points[(int)face[3]]) / 4;
            face_points.Add(new_point);
        }
        return face_points;
    }

    // Returns a list of "edge points" for the given CCMeshData, as described in the Catmull-Clark algorithm 
    public static List<Vector3> GetEdgePoints(CCMeshData mesh)
    {
        List<Vector3> edge_points = new List<Vector3>();
        foreach(Vector4 edge in mesh.edges)
        {
            Vector3 new_point = (mesh.points[(int)edge[0]] + mesh.points[(int)edge[1]] + mesh.facePoints[(int)edge[2]] + mesh.facePoints[(int)edge[3]]) / 4;
            edge_points.Add(new_point);
        }
        return edge_points;
    }

    // Returns a list of new locations of the original points for the given CCMeshData, as described in the CC algorithm 
    public static List<Vector3> GetNewPoints(CCMeshData mesh)
    {
        List<Vector3> new_points = new List<Vector3>(mesh.points);
        Vector3[] edges_avg = new Vector3[mesh.points.Count];
        int[] edges_counter = new int[mesh.points.Count];
        Vector3[] faces_avg = new Vector3[mesh.points.Count];
        int[] faces_counter = new int[mesh.points.Count];
        foreach (Vector4 edge in mesh.edges)
        {
            Vector3 avg = (mesh.points[(int)edge[0]] + mesh.points[(int)edge[1]]) / 2;
            edges_avg[(int)edge[0]] += avg;
            edges_counter[(int)edge[0]]++;
            edges_avg[(int)edge[1]] += avg;
            edges_counter[(int)edge[1]]++;
        }
        for (int i = 0; i < mesh.faces.Count; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                faces_avg[(int)mesh.faces[i][j]] += mesh.facePoints[i];
                faces_counter[(int)mesh.faces[i][j]]++;
            }
        }
        for (int i = 0; i < new_points.Count; ++i)
        {
            edges_avg[i] /= edges_counter[i];
            faces_avg[i] /= faces_counter[i];
            new_points[i] = (faces_avg[i] + 2 * (edges_avg[i]) + ((faces_counter[i] - 3) * mesh.points[i])) / faces_counter[i];
        }
        return new_points;
    }
}
