using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
[ExecuteInEditMode]

public class PolygonCollider2DConvexHull : MonoBehaviour
{
    [MenuItem("Tools/Add Polygon Collider - Convex Hull")]
    public static void AddPolygonColliderToSelectedObject()
    {
        // Get the selected sprites
        SpriteRenderer[] renderers = Selection.GetFiltered<SpriteRenderer>(SelectionMode.TopLevel);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("No sprites selected");
            return;
        }
        Debug.Log("Number of sprite renderers selected: " + renderers.Length);
  
        foreach (SpriteRenderer renderer in renderers)
        {
            Debug.Log("Sprite renderer selected: " + renderer.sprite);

            // Get the sprite points in local space
            Vector2[] spritePoints = renderer.sprite.vertices;
            foreach (Vector2 sprite in spritePoints)
            {
                Debug.Log("Sprite point in local space: " + sprite);
            }

            // Convert the list of Vector2 points to a list of Vertex2 points
            List<Vertex2> vertices = new List<Vertex2>();
            foreach (Vector2 point in spritePoints)
            {
                vertices.Add(new Vertex2(point.x, point.y));
                Debug.Log("Sprite point location: " + point);

                // Need a way to draw these points in the game scene view
            }

            Debug.Log("Total # of points: " + vertices.Count);


            // Compute the convex hull using the Jarvis March gift wrapping algorithm
            List<Vertex2> convexHull = JarvisMarch.ConvexHull(vertices);

            // Create a PolygonCollider2D component for the sprite GameObject
            PolygonCollider2D collider = renderer.gameObject.AddComponent<PolygonCollider2D>();

            // Set the points of the PolygonCollider2D to the convex hull vertices
            Vector2[] colliderPoints = new Vector2[convexHull.Count];
            for (int i = 0; i < convexHull.Count; i++)
            {
                colliderPoints[i] = new Vector2((float)convexHull[i].X, (float)convexHull[i].Y);
            }
            collider.points = colliderPoints;

            // Set trigger to True
            collider.isTrigger = true;
        }
    }
}

public class Vertex2
{
    public float  X { get; set; }
    public float Y { get; set; }
    public static Vertex2 operator -(Vertex2 a, Vertex2 b) => new Vertex2(a.X-b.X,a.Y-b.Y);
    public float SqrMagnitude() { return (X * X + Y * Y); }
    public Vertex2(float x, float y)
    {
        X = x;
        Y = y;
    }
}


// Perform the Gift Wrapping algorithm, also known as a Jarvis March
// Step 1 - sort all vertices to figure out which one has the smallest x coordinate
// if there are multiple points with the smallest x coordinate, pick the point with 
// the smallest x and z coordinate. This point is always on the convex hull
// Step 2 - loop through all other points to identify which points are on the convex hull
// to know if two points are on the convex hull, we have to check all other points and make
// sure none of them are to the right of a line between the two points
// Video for reference: https://www.youtube.com/watch?v=Vu84lmMzP2o 

// To make all this work in Unity you need random points in x-z-space and they should be stored 
// as a Vertex in a list (Create a new Vertex object with a Vector3 as its position and where y = 0). 
// You also need an algorithm to figure out if a point is to the left, to the right, or on the same line as 2 other points.

public class JarvisMarch
{
    public Vector3 point;

    public const float EPSILON = 0.00001f;

    public static float Det2(float x1, float x2, float y1, float y2)
    {
        return x1 * y2 - y1 * x2;
    }

    public static float GetPointInRelationToVectorValue(Vertex2 a, Vertex2 b, Vertex2 p)
    {
        float x1 = a.X - p.X;
        float x2 = a.Y - p.Y;
        float y1 = b.X - p.X;
        float y2 = b.Y - p.Y;

        float determinant = Det2(x1, x2, y1, y2);

        return determinant;
    }



    public static float IsPointLeftOfVector(Vertex2 a, Vertex2 b, Vertex2 p)
    {
        float relationValue = GetPointInRelationToVectorValue(a, b, p);

        bool isToLeft = true;

        //to avoid floating point precision issues we can add a small value
        if ((relationValue > 0f - EPSILON) && (relationValue < 0f + EPSILON)){ return (0); }
        return (relationValue);
    }


    // Finds the leftmost point in a list of vertices
    public static Vertex2 FindStartPoint(List<Vertex2> points)
    {
        Vertex2 startPoint = points[0];
        foreach (Vertex2 point in points)
        {
            if (point.X < startPoint.X)
            {
                startPoint = point;
            }
            else if (point.X == startPoint.X && point.Y < startPoint.Y)
            {
                startPoint = point;
            }
        }
        return startPoint;
    }



    // Calculates the angle between three points
    public static float Angle(Vertex2 a, Vertex2 b, Vertex2 c)
    {
        float x1 = b.X - a.X;
        float y1 = b.Y - a.Y;
        float x2 = c.X - b.X;
        float y2 = c.Y - b.Y;
        return (float)Math.Atan2(x1 * y2 - x2 * y1, x1 * x2 + y1 * y2);
    }

    // Implement the Jarvis March gift wrapping algorithm
    public static List<Vertex2> ConvexHull(List<Vertex2> points)
    {

        //If we have just 3 points, then they are the convex hull, so return those
        if (points.Count == 3)
        {
            //These might not be ccw, and they may also be colinear
            return points;
        }

        //If fewer points, then we cant create a convex hull
        if (points.Count < 3)
        {
            return null;
        }

        //The list with points on the convex hull
        List<Vertex2> convexHull = new List<Vertex2>();

        //Step 1. Find the vertex with the smallest x coordinate
        //If several have the same x coordinate, find the one with the smallest z
        Vertex2 startVertex = points[0];

        Vertex2 startPos = startVertex;

        for (int i = 1; i < points.Count; i++)
        {
            Vertex2 testPos = points[i];

            //Because of precision issues, we use Mathf.Approximately to test if the x positions are the same
            if ((testPos.X < startPos.X) || Mathf.Approximately(testPos.X, startPos.X) )
            {
                startVertex = points[i];
                startPos = startVertex;
            }
        }

        //This vertex is always on the convex hull
        convexHull.Add(startVertex);

        points.Remove(startVertex);



        //Step 2. Loop to generate the convex hull
        Vertex2 currentPoint = convexHull[0];

        //Store colinear points here - better to create this list once than each loop
        List<Vertex2> colinearPoints = new List<Vertex2>();

        int counter = 0;

        while (true)
        {
            //After 2 iterations we have to add the start position again so we can terminate the algorithm
            //Cant use convexhull.count because of colinear points, so we need a counter
            if (counter == 2)
            {
                points.Add(convexHull[0]);
            }

            //Pick next point randomly
            Vertex2 nextPoint = points[UnityEngine.Random.Range(0, points.Count)];

            //To 2d space so we can see if a point is to the left is the vector ab
            Vertex2 a = currentPoint;

            Vertex2 b = nextPoint;

            //Test if there's a point to the right of ab, if so then it's the new b
            for (int i = 0; i < points.Count; i++)
            {
                //Dont test the point we picked randomly
                if (points[i].Equals(nextPoint))
                {
                    continue;
                }

                Vertex2 c = points[i];

                //Where is c in relation to a-b
                // < 0 -> to the right
                // = 0 -> on the line
                // > 0 -> to the left
                float relation = IsPointLeftOfVector(a, b, c);

                //Colinear points
                //Cant use exactly 0 because of floating point precision issues
                //This accuracy is smallest possible, if smaller points will be missed if we are testing with a plane
                float accuracy = 0.00001f;

                if (relation < accuracy && relation > -accuracy)
                {
                    colinearPoints.Add(points[i]);
                }
                //To the right = better point, so pick it as next point on the convex hull
                else if (relation < 0f)
                {
                    nextPoint = points[i];

                    b = nextPoint;

                    //Clear colinear points
                    colinearPoints.Clear();
                }
                //To the left = worse point so do nothing
            }

            //If we have colinear points
            if (colinearPoints.Count > 0)
            {
                colinearPoints.Add(nextPoint);

                //Sort this list, so we can add the colinear points in correct order
                colinearPoints = colinearPoints.OrderBy(n => (n - currentPoint).SqrMagnitude()).ToList();

                convexHull.AddRange(colinearPoints);

                currentPoint = colinearPoints[colinearPoints.Count - 1];

                //Remove the points that are now on the convex hull
                for (int i = 0; i < colinearPoints.Count; i++)
                {
                    points.Remove(colinearPoints[i]);
                }

                colinearPoints.Clear();
            }
            else
            {
                convexHull.Add(nextPoint);

                points.Remove(nextPoint);

                currentPoint = nextPoint;
            }

            //Have we found the first point on the hull? If so we have completed the hull
            if (currentPoint.Equals(convexHull[0]))
            {
                //Then remove it because it is the same as the first point, and we want a convex hull with no duplicates
                convexHull.RemoveAt(convexHull.Count - 1);

                break;
            }

            counter += 1;
        }

        return convexHull;
    }
}



