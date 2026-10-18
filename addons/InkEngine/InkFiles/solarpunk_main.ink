INCLUDE solarpunk_functions.ink


->init




==init
// Define bots
~smallbot1 = (SmallBot1, Tread, Slot1_Empty, FullPower)
~smallbot2 = (SmallBot2, ATV, Slot1_Empty)
~smallbot3 = (SmallBot3, Walker, Slot1_Empty)
~smallbot4 = (SmallBot4, Hover, Slot1_Empty)
~mediumbot1 = (MediumBot1, Tread, Slot1_Empty, Slot2_Empty)
~mediumbot2 = (MediumBot2, ATV, Slot1_Empty, Slot2_Empty)
~largebot1 = (LargeBot1, Walker, MISC_SecurityBot)
//Define starting bot(s)
~ownedBots = (SmallBot1)
// List of all possibl bots
~allBots = LIST_ALL(small_bots) + LIST_ALL(medium_bots) + LIST_ALL(large_bots)
// Define starting equipment
~allEquipment = LIST_ALL(eq_strength) + LIST_ALL(eq_dexterity) + LIST_ALL(eq_constitution) + LIST_ALL(eq_intelligence) + LIST_ALL(eq_wisdom) + LIST_ALL(eq_charisma) + LIST_ALL(eq_misc)
// What we own!
~ownedEquipment = (STR_Excavator, WIS_Scanner, INT_WaterLab)

//Define all tasks
~task_botbay_00 = (TaskControlled, TaskNotStarted, TaskMaxProgress10)
~task_earthquake_01 = (TaskRisky, TaskNotStarted, TaskMaxProgress3, STR_Excavator)

->start

==start
+ [Meet customer]
->customer_test
+ [CUSTOM_BUTTON(EquipBot) Equip Bot]
->EquipBot(->start)

+ [CUSTOM_BUTTON(InspectBot) Inspect Bot]
->InspectBot(->start)

+ [CUSTOM_BUTTON(UnEquipBot) Unequip Bot]
->UnequipBot(->start)

+ [CUSTOM_BUTTON(InspectEquipment) Equipment]
->PickEquipment(->start, ownedEquipment)

==customer_test
Hello I need an excavator bot. My task is {Task_01_Earthquake}.
~currentTasks+=Task_01_Earthquake
+ [Assign bots.]
->AssignBotsToTask(Task_01_Earthquake)->customer_test
+ [Continue.]
->end_day_test

==end_day_test
->ProgressTask(Task_01_Earthquake)->
The task is now {GetTask(Task_01_Earthquake)}.

+ [Back to start.]
->start