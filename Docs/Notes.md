# Notes for future game dev

- In case of pixelated game dev, change godot settings in window to   
	--> settings --> display --> window --> Stretch --> viewport  
	it helps when rescaling the canvas so that pixel graphics are stretched with it, without altering resolution/ texture of pixel graphics

- the order in which the resources appear in the scene, are the order in which Godot draws them on the canvas, starting from top to bottom (works even for nested resources). This means, resource that is on top of the rest has to be in lowest position

  ![alt text](image.png)

- A **HashSet** is a data structure wherein each elemetn of the HashSet is going to be unique (for example Vector2)... If one adds same HashSet ( for example one occupied set as a Vector2) it won't add a same one if it included again 

- Switching e.g. Main Node (where we would add all level resources) from Node2D (which has positional arguments) to a simple Node type, helps to avoid the accidental movement of the whole level positioning. Main is so used as common ancestor for each child nodes, therefore it deosn't need positional data

