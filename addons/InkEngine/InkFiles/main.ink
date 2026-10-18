INCLUDE functions.ink
/* Commands.
#SET_POS:box_1
#SET_BUBBLEPOS:face_1^chain_1 (for bubblechain intro, ignore otherwise)
#SET_CAMERA_TARGET:box_1^1.2^1
#SET_BUBBLECHAIN:chain_1 (to add to existing bubblechain)
#SET_CUSTOM_LABEL:SpeechBubble_Talk (for speech bubble)
*/
VAR isPlaying = false

==init
 #SET_SPRITEBACKGROUND:scene_main #SET_SPRITESUBTEXTURE:city

The year is 20XX. {B("box_1", 2, 5)} #PLAY_MUSIC:MusicCalm

The [b]War[/b] between [b]Machines[/b] and [b]Men[/b] is[br]in the distant past... {B("box_2", 3, 1)}

And as the [b]clouds of radiation[/b] have finally[br]started dissipating from the blasted cities... {B("box_3", 2, 1)}

[b]Scavengers[/b] - like you - have begun[br]to loot the [b]OldTek[/b] hidden in the ruins. {B("box_4", 3, 1)}

There is just one problem. #clear

Sooo the ruin's full of robots.. {S(Deya, Neutral2)}

So? Just don't get seen. {S(Zayne, Neutral1)}

That's easy for you to say! {S(Deya, Angry)}

You're not the one going in there! {S(Deya, Angry)}

Relaaax. You're faster than they are. Just don't get stuck in a corner with one. {S(Zayne, Smile1)}

Besides, they forget about you the moment they lose sight of you. So just hide. {S(Zayne, Smirk1)}

Ugh. What are we looking for anyway? {S(Deya, Neutral1)}

The old man gave us a list, but...I dunno what half of these things are. {S(Zayne, Sad)}

Let me see that. {S(Deya, Angry)}

...well. Shit. OldTek indeed. {S(Deya, Sad)}

Let's just start looking. We'll make an educated guess once we have something. {S(Zayne, Smirk2)}

Fine. But you better pull your weight! {S(Deya, Smirk1)}

{ClearPortraits()}

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
+ [Load level]
Loading level 1. #LOAD_LEVEL:level_1 #clear #PLAY_MUSIC:MusicCombat
->DONE

==continuePlay
And we're back!

Hooray!

+ [Back to start]
->init

==story_knot_test
This is a story knot! WooooOOoo! #SET_CAMERA_TARGET:Default^1^0 #SET_SPRITESUBTEXTURE:ruin

+ [Rock on.]
We rock on.
+ [WOOOO]
Woo. #continue
-
#UNPAUSE_LEVEL #continue #SET_SPRITESUBTEXTURE:ruin^false
->DONE


