var R = 30;
var S = 1.5 * R;
var H = Math.sqrt(3) * R;
var cellGrid = [];
var fps = 0;
var distX = 0, distY = 0;
var loc = [];
var defaultFillColor = [72, 72, 72];
var currentlyHighlighted = null;
var vpMinCorner = [], vpMaxCorner = [];
window.onresize = updateSize;
var previousChunkInfo = [];

var idStorage = [];

var simID = 0;

var simLaunched = false;

function mouseDragged() {
	distX += (mouseX - pmouseX);
	distY += (mouseY - pmouseY);
}

function updateSize() {
	resizeCanvas(windowWidth, windowHeight);
}

function setup() {
	createCanvas(windowWidth, windowHeight);
	background(18, 18, 18);
	textSize(12);

	for (let i = 0; i < 200; i++)
		for (let j = 0; j < 200; j++)
			cellGrid.push(new HexCell(j, i));

	console.log(cellGrid);
}

function prev() {
	if (document.getElementById("simID").value != 0) {
		document.getElementById("simID").value--;
		document.getElementById("simID").onchange();
	}
}

function next() {
	document.getElementById("simID").value++;
	document.getElementById("simID").onchange();
}

async function requestRound() {
	// https://localhost:44339/api/GetSimulation?id=1284113452&round=1&x=0&y=0
	let response = await fetch("api/GetSimulation?id=" + simID["simulationID"] + "&round=" + document.getElementById("simID").value + "&x=0&y=0");
	console.log(response);
	let myJson = await response.json();

	await updateChunk3(cellGrid, myJson);
	// await updateChunk(cellGrid, myJson);
}

async function initSimulation(roundcount) {
	let simID = 0;
	if (!simLaunched) simID = await fetch("api/StartSimulation/" + roundcount);
	simLaunched = true;
	return simID;
}

async function loadServerData() {
	simID = await initSimulation(200);
	simID = await simID.json();
	console.log(simID["simulationID"]);

	let response = await fetch("api/GetSimulation?id=" + simID["simulationID"] + "&round=0&x=0&y=0");
	console.log(response);
	let myJson = await response.json();

	await updateChunk3(cellGrid, myJson);
	document.getElementById("simPager").style.visibility = "visible";
}

function draw() {
	vpMinCorner = findCellAt(0 - 2 * R - distX, 0 - 2 * R - distY);
	vpMaxCorner = findCellAt(windowWidth + 2 * R - distX, windowHeight + 2 * R - distY);

	background(36, 36, 36);
	stroke(144, 144, 144);
	strokeWeight(1);
	cellGrid.filter(elem => elem.gridRefX >= vpMinCorner[0] &&
							elem.gridRefX <= vpMaxCorner[0] &&
							elem.gridRefY >= vpMinCorner[1] &&
							elem.gridRefY <= vpMaxCorner[1])
			.forEach(cell => cell.draw(distX, distY));

	loc = findCellAt(mouseX - distX, mouseY - distY);
	cellGrid.filter(elem => elem.gridRefX == loc[0] && elem.gridRefY == loc[1])
			.forEach(cell => cell.draw(distX, distY, [96, 96, 128]));

	stroke(0);
	strokeWeight(2);
	fill(255);
	fps = frameRate();
	textAlign(LEFT, TOP);
	textSize(12);
	text("FPS: " + fps.toFixed(2) + ", Location: (" + loc[0] + ", " + loc[1] + ")", 10, windowHeight - 20);
}

function findCellAt(mX, mY) {
	// kimeneti változók
	let outX = 0, outY = 0;

	// ha "négyzetrácsokból" állna a grid, az (it, jt) vektor a
	// grid-relatív koordinátákat adná meg.
	let it = Math.floor(mX / S);
	let yts = mY + (it % 2) * (H / 2);
	let jt = Math.floor(yts / H);

	// ez a "négyzetrácson" belüli relatív pozíciót adja meg
	let xt = mX - it * S;
	let yt = yts - jt * H;

	if (xt > R * (0.5 - yt / H)) {
		outX = it;
		outY = jt;
	} else if (yt > H / 2) {
		outX = it - 1;
		outY = jt + outX % 2;
	} else {
		outX = it - 1;
		outY = jt + outX % 2 - 1;
	}

	return [outX, outY];
}

class HexCell {
	constructor(x, y) {
		this.gridRefX = x;
		this.gridRefY = y;

		this.uid = -1;
		this.content = "";
		this.wiring = [];
		this.startState = -1;
		this.parentID = -1;
		this.health = -1;

		this.vertices = this.precalc(x, y);
	}

	precalc(gridX, gridY) {
		let vertexStorage = [];
		let startX = gridX * S;
		let startY = gridY * H + H / 2 - (gridX % 2) * (H / 2);

		vertexStorage.push([startX, startY]);
		vertexStorage.push([startX + 0.5 * R, startY - H / 2]);
		vertexStorage.push([startX + 1.5 * R, startY - H / 2]);
		vertexStorage.push([startX + 2.0 * R, startY]);
		vertexStorage.push([startX + 1.5 * R, startY + H / 2]);
		vertexStorage.push([startX + 0.5 * R, startY + H / 2]);

		return vertexStorage;
	}

	draw(offsetX = 0, offsetY = 0, fillColor = defaultFillColor) {
		beginShape();
		fill(fillColor[0], fillColor[1], fillColor[2]);
		for (let i = 0; i < this.vertices.length; i++)
			vertex(this.vertices[i][0] + offsetX, this.vertices[i][1] + offsetY);
		endShape(CLOSE);

		if (this.content !== "") {
			textSize(R);
			fill(255);
			textAlign(CENTER, CENTER);
			text(this.content, this.vertices[0][0] + R + offsetX, this.vertices[0][1] + offsetY);
		}
	}
}

async function updateChunk(grid, src) {
	grid.forEach(elem => { elem.content = ""; });
	grid.forEach(elem => {
		src.filter(input => input.X == elem.gridRefX && input.Y == elem.gridRefY)
		   .forEach(item => {
				if (item.Type == "t") elem.content = "🌳";
				else if (item.Type == "l") elem.content = "🦁";
				else if (item.Type == "a") elem.content = "🧠";
		   })
	});
}

async function updateChunk2(grid, src) {
	document.getElementById("simID").setAttribute('onchange', '');
	document.getElementById("prevButton").setAttribute('onchange', '');
	document.getElementById("nextButton").setAttribute('onchange', '');
	console.log(src);

	// ISSUE: backend resends cells that didn't move also, needs fixing

	previousChunkInfo.forEach(item => {
		grid.find(elem =>
			elem.gridRefX == item.X && elem.gridRefY == item.Y).content = "";
	});

	src.forEach(item => {
		let objReference = grid.find(elem => 
			elem.gridRefX == item.X && elem.gridRefY == item.Y);

		if (item.Type == "t") objReference.content = "🌳";
		else if (item.Type == "l") objReference.content = "🦁";
		else if (item.Type == "a") objReference.content = "🧠";
	});

	previousChunkInfo = src;
	document.getElementById("simID").setAttribute('onchange', 'requestRound()');
	document.getElementById("prevButton").setAttribute('onchange', 'prev()');
	document.getElementById("nextButton").setAttribute('onchange', 'next()');
}

async function updateChunk3(grid, src) {
	// make buttons inactive, log incoming json data
	document.getElementById("simID").setAttribute('onchange', '');
	document.getElementById("prevButton").setAttribute('onchange', '');
	document.getElementById("nextButton").setAttribute('onchange', '');
	console.log(src);

	if (document.getElementById("simID").value == 0) {
		// initial state esetén a data után kajtatunk
		src.data.forEach(cell => {
			let objReference = grid.find(elem => 
				elem.gridRefX == cell.X && elem.gridRefY == cell.Y);

			if (cell.Type == "t") objReference.content = "🌳";
			else if (cell.Type == "l") objReference.content = "🦁";
			else if (cell.Type == "a") {
				objReference.content = "🧠";
				objReference.wiring = cell.wiring;
				objReference.startState = cell.startState;
				objReference.health = 50;
			}

			objReference.uid = cell.UID;
			idStorage.push(cell.UID);
		});
	} else {
		// subsequent statek esetén a movements, spawns, deaths,
		// healthupdates után kajtatunk

		let objReference = null;

		/*src.movements.forEach(cell => {
			objReference = grid.find(elem => elem.uid == cell.UID);

			let tempReference = grid.find(elem => 
				elem.gridRefX == cell.X && elem.gridRefY == cell.Y);

			if (tempReference) {
				tempReference.content = objReference.content;
				tempReference.uid = objReference.uid;
				tempReference.parentID = objReference.parentID;
				tempReference.startState = objReference.startState;
				tempReference.wiring = objReference.wiring;
				tempReference.health = objReference.health;

				objReference.content = "";
				objReference.uid = -1;
				objReference.parentID = -1;
				objReference.startState = -1;
				objReference.wiring = [];
				objReference.health = -1;
			}
		});*/

		src.movements.forEach(cell => {
			//console.log("Attempting to move " + cell.UID + " at (" + cell.X + ", " + cell.Y + ")...");
			let success = false;

			if (!idStorage.find(e => e == cell.UID)) console.error("Illegal automaton reference detected (UID: " + cell.UID + ")");

			grid.filter(elem => elem.uid == cell.UID).forEach(find => {
				grid.filter(nextelem => nextelem.gridRefX == cell.X && nextelem.gridRefY == cell.Y)
					.forEach(next => {
						/*console.log("Moving " + find.uid + " from (" + find.gridRefX + ", " + find.gridRefY + ") to ("
							+ next.gridRefX + ", " + next.gridRefY + ")...");*/

						success = true;

						// find: a megtalált hexcell ha létezik
						// next: az új hexcell amibe mozog, ha létezik
						next.content = find.content;
						next.uid = find.uid;
						next.parentID = find.parentID;
						next.startState = find.startState;
						next.wiring = find.wiring;
						next.health = find.health;

						// az előző értékeit alaphelyzetbe állítjuk
						find.content = "";
						find.uid = -1;
						find.parentID = -1;
						find.startState = -1;
						find.wiring = [];
						find.health = -1;
					})
			})

			if (!success) console.error("Moving " + cell.UID + " to (" + cell.X + ", " + cell.Y + ") failed.");
		});

		src.spawns.forEach(cell => {
			objReference = grid.find(elem => 
				elem.gridRefX == cell.X && elem.gridRefY == cell.Y);
			
			objReference.content = "🧠";
			objReference.uid = cell.childUID;
			objReference.parentID = cell.parentUID;
			objReference.startState = cell.startState;
			objReference.wiring = cell.wiring;
			objReference.health = 50;

			idStorage.push(cell.childUID);
		});

		src.deaths.forEach(cell => {
			objReference = grid.find(elem => 
				elem.gridRefX == cell.X && elem.gridRefY == cell.Y);
			
			objReference.content = "";
			objReference.uid = -1;
			objReference.parentID = -1;
			objReference.startState = -1;
			objReference.wiring = [];
			objReference.health = -1;

			if (!idStorage.find(e => e == cell.UID)) console.error("Illegal automaton reference detected (UID: " + cell.UID + ")");
			else idStorage = idStorage.filter(item => item != cell.UID);
		});

		src.healthUpdates.forEach(cell => {
			let success = false;

			if (!idStorage.find(e => e == cell.UID)) console.error("Illegal automaton reference detected (UID: " + cell.UID + ")");

			grid.filter(elem => elem.uid == cell.UID).forEach(find => {
				find.health = cell.health;
				success = true;
			});

			if (!success) console.error("Updating the health of automaton " + cell.UID + " to " + cell.health + " failed.");
		});
	}

	document.getElementById("simID").setAttribute('onchange', 'requestRound()');
	document.getElementById("prevButton").setAttribute('onchange', 'prev()');
	document.getElementById("nextButton").setAttribute('onchange', 'next()');
}