# Interactive-Narrative-
###[Online demo (desktop only)] (http://raycoti.com/index.php/2016/09/21/interactive-narrative-demo/)

#Table of contents
###[Background](#background)
###[Main Prefabs](#prefabs)
###[Models](#models)
###[Scripts](#scripts)

<a name = "background"/>
##Background
This was my senior capstone project. I originally wanted to create a short film   

###Phases
- [x] Create models, scipts and animations
- [x] Manually crete enviornment
- [x] Implement network play
- [x] Have it run online
- [ ] Have online builds implement network play
- [ ] Allow for users to update text of enviorment
- [ ] Create a scene builder

<a name = "prefabs"/>
##Main Prefabs
###Textrenderer
This prefab contains the appropriate colliders and text assets to create and place exerpts in the enviornment. 
changemessage.cs takes care of cycling through the strings of text you want users to read when they enter the space. You can edit the number of messages, and the messages themselves in the unity build by changing the values in the words and words2 array strings. My project in particular had two messages for each object, one that only the person interacting with the space could read and another that everyone else could read by looking at the message in the sky. If you do not want this additional text just leave words2 blank. 
[textrenderer](https://github.com/raycoti/Interactive-Narrative-/tree/master/Assets/interactive%20text)

###Moveyo
This prefab is simply a collider that utilizes [moveplayer.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/moveplayer.cs) to transport players around the enviornment. In my enviornment these were placed in the trains so that when a player entered a train they walk out of another train. In the unity build drag the gameobject you want the collider to transfer the player to in the "gotothis" variable. 

###networkfirst
This is a modefied firstperson character controller that handles multiple users for online play. The simple script [setuplocalplayer.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/Standard%2520Assets/Characters/FirstPersonCharacter/Scripts/SetUpLocalPlayer.cs) enables the first person camera. By having all cameras not enabled, and only enabling the appropriate appropriate camera we avoid the problem of being able to control all users' cameras. 
<a name = "models" / >
##Models
These are the models I created in Maya. The models are of my college's library, science building and student center. Areas in Chicago include the trainstation closest to my house and train underpasses that populate my neighborhood. 
[models/animations](https://github.com/raycoti/Interactive-Narrative-/tree/master/Assets/Models%20Animation)
<a name = "scripts" />
##other scripts
[creator.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/creator.cs) may seem like it only spawns a random forest of blocks, but it has the foundation makeup of generating an enviornment! The entire enviornment will be mapped to a grid system, each prefab and model will be placed using this grid. 
