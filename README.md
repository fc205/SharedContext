# UiPathTeam.SharedContext.Activities
Custom UiPath Activities that allow for creation of a Shared context scope and running commands against it.

<b>Summary</b>

Allows establishing a Shared context and accessing it.

<b>Benefits</b>

Useful for IPC (Inter Process Communication). 

<b>Package specifications</b>	

Contains three custom activities that facilitate establishing IPC and sharing variables through it.

Please note the following limitations at this point:
- only File-based IPC is implemented (Memory-Mapped Files or Named Pipes are likely candidates for implementation in newer versions)
- variables can only be of String type as of yet

<b>SharedContextScopeActivity:</b>

InArguments
<string> Name (required): Unique name of the context
<contextType> Type (required): Only File is possible at the moment
<bool> Clear (required): Clears the context at the beggining of the Scope
<bool> Lock (required): Take a lock on the Context resource
<int> Retries: # of times that the opening of the File Context resource will be attempted
<string> Folder: Folder where the Context File will be placed (defaults to System.Path.GetTempPath())

OutArguments
<string> FilePath: Full path of the file that is created to store the context

<b>SharedContextSetActivity:</b>

InArguments
<string> Name (required): Name of the variable to be set
<string> Value (required): String Value of the variable to be set

<ContextClient> ContextClient - provide this if you would prefer to create your own ContextClient rather than use the one inside a SharedContextScopeActivity.

<b>SharedContextGetActivity:</b>

InArguments
<string> Name (required): Name of the variable to be gotten
<bool> RaiseException (required): whether the activity will raise an exception if the variable is not available in the context (wasn't set first)

<ContextClient> ContextClient - provide this if you would prefer to create your own ContextClient rather than use the one inside a SharedContextScopeActivity.

OutArguments
<string> Value: The String Value of the variable from the context

<b>Dependencies</b>

Newtonsoft.Json >= 12.0.3

<b>Installation guide</b>

Install the UiPathTeam.SharedContext.Activities.1.0.10.nupkg using the Package Manager
