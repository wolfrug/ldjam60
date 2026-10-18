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

The year is 2001. {B("box_1", 2, 5)} #PLAY_MUSIC:MusicCalm

It has been twenty years since the [b]War[/b] between [b]Machines[/b] and [b]Men[/b][br]and the Earth is still recovering. {B("box_2", 3, 1)}

But as the [b]clouds of radiation[/b] have finally[br]started dissipating from the blasted cities... {B("box_3", 2, 1)}

...[b]Scavengers[/b] - like you - have begun[br]to loot the [b]OldTek[/b] hidden in the ruins. {B("box_4", 3, 1)}

There is just one problem. #clear

Sooo the ruin's full of robots.. {S(Deya, Neutral2)}

Your companion, Zayne, scoffs at you.

So? Just don't get seen. {S(Zayne, Neutral1)}

That's easy for you to say! {S(Deya, Angry)}

Zayne is a fixer, appraiser and driver. Not a rune-delver. Yes, he's important. No, it doesn't make it any less unfair.

You're not the one going in there! {S(Deya, Angry)}

Relaaax. You're faster than they are. Just don't get stuck in a corner with one. {S(Zayne, Smile1)}

If you do, they will slice you to tiny bits with their still-functioning scythe arms.

Besides, they forget about you the moment they lose sight of you. So just hide. {S(Zayne, Smirk1)}

Ugh. What are we looking for anyway? {S(Deya, Neutral1)}

The old man gave us a list, but...I dunno what half of these things are. {S(Zayne, Sad)}

Let me see that. {S(Deya, Angry)}

->toDoList->
...well. Shit. OldTek indeed. {S(Deya, Sad)}

Let's just start looking. We'll make an educated guess once we have something. {S(Zayne, Smirk2)}

Fine. But you better pull your weight! {S(Deya, Smirk1)}

{ClearPortraits()} #clear

It is time to venture into the ruins and look for some OldTek! A few notes: use WASD to move. Space to interact. If you meet any nasty robots, don't let them hit you! You can't fight, but you can run. The goal is to collect all the items in the level and then leave. Good luck!
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


