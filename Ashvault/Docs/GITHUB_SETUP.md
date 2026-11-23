# GitHub setup: SquishieInc/Ashvault

The linked GitHub identity resolves to **SquishieInc**, but the app currently exposes **zero repositories** to this session. This expansion has **not** been uploaded. The connector has repository file/branch/commit actions but not a repository-creation action, so a repository must be created and granted access first.

## Create and push from your Mac

1. Create a private `Ashvault` repository under `SquishieInc` at https://github.com/new (leave the GitHub-generated README, .gitignore and licence unchecked).
2. Open the archive through Unity 6.6 Universal 3D, let the Editor import all assets and generate their `.meta` files. Save `Assets/Ashvault/Scenes/Prototype.unity` using **Tools > Ashvault > Create or Rebuild Prototype Scene**.
3. From the **Unity project root** in Terminal:

```sh
git init
git add Assets Packages ProjectSettings Docs README.md .gitignore .gitattributes
git commit -m "Implement Ashvault nine-map campaign, eight classes and enemy AI"
git branch -M main
git remote add origin https://github.com/SquishieInc/Ashvault.git
git push -u origin main
```

If you copied only the `Assets/Ashvault` folder into a new URP project, adapt the above `git add` paths accordingly. In a typical Unity project, `Assets`, `Packages`, `ProjectSettings`, docs and asset `.meta` files belong in Git. Generated `Library`, `Temp`, `Obj`, `Logs`, builds and personal preferences do not.

**Alternative:** from the project root, when authenticated with GitHub CLI, `gh repo create SquishieInc/Ashvault --private --source=. --remote=origin --push` can create the private repository and push. Use either route, not both.

4. In GitHub's ChatGPT app/connection access settings, grant the app access to **SquishieInc/Ashvault** (if needed). Once the repository is visible, subsequent development can be organized into feature branches and reviewed pull requests.

Do not share GitHub tokens/passwords in chat. You may choose GitHub LFS for future large `.psd`, `.fbx`, `.wav`, `.blend` or `.mp4` files; it is not necessary for the small prototype textures.
