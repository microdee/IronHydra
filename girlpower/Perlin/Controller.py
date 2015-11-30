# MetaForVVVV: Controller.ihm.json

def execute():
    wait = true;
    while wait:
        done = true;
        for rowdone in Result.Content["RowDone"]:
            done = done and rowdone
        if done:
            wait = false
    for i in range(0, len(Result.Content["Values"])):
        Populate.Content["Values"][i] = Result.Content["Values"][i]

    for thread in Neighbourhood:
        thread.Run()

execute()
