# 360PlayerPrototype
Prototype of a 360 video player in Unity

# Credits: 
The project contains scripts based off of the Oculus Sample Framework (https://developer3.oculus.com/downloads/game-engines/1.5.0/Oculus_Sample_Framework_for_Unity_5_Project/) and uses 360Rize's Model Cannon Firing video (http://www.360heros.com/vr/) - each of which are free for download.

# Usage:
- Obtain your 360 video in both mp4 and OGV format. 
- Place both the mp4 and OGV files in the "StreamingAssets" folder of the project.
- Open the "360Player" scene and select the MovieSurface GameObject.
- Change the "Movie Name" field of the "Movie Player Sample" script to the name of your mp4 file in the StreamingAssets folder. For example, "YourMP4File.mp4"
- Build and run the "360Player"

# Known Issues:
- The project is only known to work with videos under 1 minute in length - I did not have access to others to test with.
- Because the normals are flipped on the sphere that plays the movie, text/symbols/objects in which orientation matters will appear backwards.
