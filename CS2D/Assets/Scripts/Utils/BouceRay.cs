using System.Collections.Generic;
using UnityEngine;
 
// Represents a set of bounced rays.
public class BounceRay
{
    // BounceRay result state.
 
    public List<Vector3> endPoints; 
    public List<RaycastHit> contacts;
    public bool bounced;
    public Vector3 finalDirection;
 
    // Returns all contact points from a bouncing ray at the specified position and moving in the specified direction.
    public static BounceRay Cast(Vector3 position, Vector3 direction, float magnitude)
    {
        // Initialize the return data.
        BounceRay bounceRay = new BounceRay
        {
            contacts = new List<RaycastHit>(),
            endPoints = new List<Vector3>(),
            finalDirection = direction.normalized
        };
 
        // If there is magnitude left...
        if (magnitude > 0)
        {
            Ray ray = new Ray(position, direction);
            
            // Fire out initial vector.
            RaycastHit hit;

            // Calculate our bounce conditions.
            bool hitSucceeded = Physics.Raycast(ray, out hit, magnitude);
            bool magnitudeRemaining = hit.distance < magnitude;
 
            // Get the final position.
            Vector3 finalPosition = hitSucceeded ? hit.point : position + direction.normalized * magnitude;
 
            // Draw final position.
            Debug.DrawLine(position, finalPosition, Color.green);
 
            // If the bounce conditions are met, add another bounce.
            if (hitSucceeded && magnitudeRemaining)
            {
                // Add the contact and hit point of the raycast to the BounceRay.
                bounceRay.contacts.Add(hit);
                bounceRay.endPoints.Add(hit.point);
 
                // Reflect the hit.
                Vector3 reflection = Vector3.Reflect((hit.point - position).normalized, hit.normal);
 
                // Create the reflection vector
                Vector3 reflectionVector = reflection;
 
                // Bounce the ray.
                BounceRay bounce = Cast(
                    hit.point,
                    reflectionVector,
                    magnitude - hit.distance);
 
                // Include the bounce contacts and origins.
                bounceRay.contacts.AddRange(bounce.contacts);
                bounceRay.endPoints.AddRange(bounce.endPoints);
 
                // Set the final direction to what our BounceRay call returned.
                bounceRay.finalDirection = bounce.finalDirection;
 
                // We've bounced if we are adding more contact points and origins.
                bounceRay.bounced = true;
            }
            else
            {
                // Add the final position if there is no more magnitude left to cover.
                bounceRay.endPoints.Add(finalPosition);
                bounceRay.finalDirection = direction;
            }
        }
 
        // Return the current position & direction as final.
        return bounceRay;
    }
}