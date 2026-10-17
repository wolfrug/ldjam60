/* Commands.
#SET_POS:box_1
#SET_BUBBLEPOS:face_1^chain_1 (for bubblechain intro, ignore otherwise)
#SET_CAMERA_TARGET:box_1^1.2^1
#SET_BUBBLECHAIN:chain_1 (to add to existing bubblechain)
#SET_CUSTOM_LABEL:SpeechBubble_Talk (for speech bubble)
*/
VAR isPlaying = false

==init
Now let's change the text here and see what happens, practically speaking. It updates. All the time.

Anyway. #SET_SPRITEBACKGROUND:example_scene

We wait a second. #SET_SPRITESUBTEXTURE:icon^true^poi_1

+ [Start over]
->init
+ {!isPlaying} [Play some music]
Let's play some music. #PLAY_MUSIC:MusicCalm
~isPlaying = true
->init
+ {isPlaying} [Stop music]
All right, enough. #PLAY_MUSIC:None
~isPlaying = false
->init
+ [Start Ambience]
#PLAY_AMBIENCE:AmbienceWind
Wind wind!
->init
+ [Stop Ambience]
#PLAY_AMBIENCE:None
No wind.
->init


===function SpeechBubble(target, bubblepos, chain)
// Usage: {SpeechBubble(face_1, bubble_1, chain_1) OR leave chain_1 as "" for no chain
{chain!="":#SET_BUBBLEPOS:{target}^{chain}|#SET_BUBBLEPOS:{target}} #SET_POS:{bubblepos}^true #SET_CUSTOM_LABEL:SpeechBubble_Talk