namespace FsMathFunctions

/// Module containing geometry functions
module Geometry =
    /// Calculate the area of a circle with given radius
    let circleArea radius = System.Math.PI * radius * radius
    
    /// Calculate the circumference of a circle with given radius
    let circleCircumference radius = 2.0 * System.Math.PI * radius
    
    /// Calculate the area of a rectangle
    let rectangleArea width height = width * height
    
    /// Calculate the volume of a sphere with given radius
    let sphereVolume radius = (4.0 / 3.0) * System.Math.PI * radius * radius * radius
    
    /// Calculate the distance between two points in 2D space
    let distance (x1, y1) (x2, y2) =
        let dx = x2 - x1
        let dy = y2 - y1
        sqrt (dx * dx + dy * dy)
