# Interactive-Narrative-
###[Online demo (desktop only)] (http://raycoti.com/index.php/2016/09/21/interactive-narrative-demo/)

###[Gallery](http://raycoti.com/index.php/2016/09/07/ct-senior-project/)

#Table of contents
##[Background](#background)
##[Overview](#overview)
##[Main Prefabs](#prefabs)
##[Models](#models2)
##[Scripts](#scripts2)

<a name = "background"/>
##Background
This was my senior capstone project. The project presentation had my application running on 4 laptops exploring the same environment  together while the laptops stremed their displays on a larger screen The future of this project focuses on the power of personal narrative working along with the power of immersion. I want to develop a new way for people to tell and share stories.      

###Phases
- [x] Create models, scipts and animations
- [x] Manually crete environment 
- [x] Implement network play
- [x] Have it run online
- [ ] Have online builds implement network play
- [ ] Allow for users to update text of environment 
- [ ] Create a scene builder

<a name = "overview"/>
##Overview
###Technical:
Up to 15 users can explore this environment  I created. You walk around with the standard "wasd" and mouse controls. Walk around the environment or go in a train to enter a new location faster. Along the way you will find boxes with floating text, simply walk into the box and the text will start cycling through parts of a poem. Each time you interact with a word cloud in the sky will also be changing. Future iterations will allow for you to submit your own messages to the word cloud for others to see. 
###Artistic:
This piece is about the factors that change our perception of others. It centers on the parallel between artwork and people: The context a piece of art is presented in will influence our judgement of its worth. If a piece of graffiti is in an art gallery instead of on a building wall it will be seen as art instead of vandalism. In what instances do we appear to be vandalism as opposed to artwork? When we don't explicitly carry our  gold plaques(degree/certificates) will others assume us to be the latter or the former?  
<a name = "prefabs"/>
##Main Prefabs
###Textrenderer
This prefab contains the appropriate colliders and text assets to create and place exerpts in the environment . 
changemessage.cs takes care of cycling through the strings of text you want users to read when they enter the space. You can edit the number of messages, and the messages themselves in the unity build by changing the values in the words and words2 array strings. My project in particular had two messages for each object, one that only the person interacting with the space could read and another that everyone else could read by looking at the message in the sky. If you do not want this additional text just leave words2 blank. 
[textrenderer](https://github.com/raycoti/Interactive-Narrative-/tree/master/Assets/interactive%20text)

###Moveyo
This prefab is simply a collider that utilizes [moveplayer.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/moveplayer.cs) to transport players around the environment . In my environment  these were placed in the trains so that when a player entered a train they walk out of another train. In the unity build drag the gameobject you want the collider to transfer the player to in the "gotothis" variable. 

###networkfirst
This is a modefied firstperson character controller that handles multiple users for online play. The simple script [setuplocalplayer.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/Standard%2520Assets/Characters/FirstPersonCharacter/Scripts/SetUpLocalPlayer.cs) enables the first person camera. By having all cameras not enabled, and only enabling the appropriate appropriate camera we avoid the problem of being able to control all users' cameras. 
<a name = "models2"/>
##Models
These are the models I created in Maya. The models are of my college's library, science building and student center. Areas in Chicago include the trainstation closest to my house and train underpasses that populate my neighborhood. 
[models/animations](https://github.com/raycoti/Interactive-Narrative-/tree/master/Assets/Models%20Animation)
<a name = "scripts2"/>
##other scripts
[creator.cs](https://github.com/raycoti/Interactive-Narrative-/blob/master/Assets/creator.cs) may seem like it only spawns a random forest of blocks, but it has the foundation makeup of generating an enviornment! The entire enviornment will be mapped to a grid system, each prefab and model will be placed using this grid. 
