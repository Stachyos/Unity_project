using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Grid))]
public class AStarPathfinder : MonoBehaviour
{
    /// <summary>
    /// Chatgpt o4-mini helped me to write and debug this script.
    /// </summary>

    public float nodeSize = 1f;

   
    public string obstacleLayerName = "Ground";


    public int gridWidth = 300;


    public int gridHeight = 200;


    public int forcedMinX = -101;


    public int forcedMinY = -36;


    public Collider2D bossCollider;

    private Vector3 originPosition; 
    private bool[,] walkable;
    private Node[,] nodes;
    private List<Tilemap> obstacleTilemaps = new List<Tilemap>();

    private class Node
    {
        public int x, y;
        public bool walkable;
        public int gCost, hCost;
        public Node parent;
        public int fCost => gCost + hCost;

        public Node(int _x, int _y, bool _walkable)
        {
            x = _x;
            y = _y;
            walkable = _walkable;
            gCost = int.MaxValue;
            hCost = 0;
            parent = null;
        }
    }

    void Awake()
    {

        if (bossCollider == null)
        {
            Debug.LogWarning("[A*] bossCollider not set");
        }
        else
        {
            Debug.Log($"[A*] set bossCollider: {bossCollider.name}");
        }
        
        obstacleTilemaps.Clear();
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap tm in allTilemaps)
        {
            if (tm.gameObject.layer == LayerMask.NameToLayer(obstacleLayerName))
            {
                obstacleTilemaps.Add(tm);
                Debug.Log($"[A*] Tilemap: {tm.name}");
            }
        }

        if (obstacleTilemaps.Count == 0)
        {
            Debug.LogWarning($"[A*] no Layer '{obstacleLayerName}' Tilemap！");
        }
        else
        {
            Debug.Log($"[A*] {obstacleTilemaps.Count} Tilemap");
        }
        
        CalculateGridBounds();
        Debug.Log($"[A*] originPosition = {originPosition}");
        
        BuildWalkableGrid();
    }

    private void CalculateGridBounds()
    {
        Vector3Int forcedMinCell = new Vector3Int(forcedMinX, forcedMinY, 0);
        Grid parentGrid = GetComponent<Grid>();
        originPosition = parentGrid.CellToWorld(forcedMinCell);

        walkable = new bool[gridWidth, gridHeight];
        nodes    = new Node[gridWidth, gridHeight];
    }

    private void BuildWalkableGrid()
    {
        if (obstacleTilemaps.Count == 0)
            return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                walkable[x, y] = true;
            }
        }

        Grid parentGrid = GetComponent<Grid>();
        
        List<Vector2Int> rawBlockedCells = new List<Vector2Int>();
        foreach (Tilemap tm in obstacleTilemaps)
        {
            BoundsInt bds = tm.cellBounds;
            foreach (Vector3Int cellPos in bds.allPositionsWithin)
            {
                if (!tm.HasTile(cellPos))
                    continue;

                // Calculate the world coordinates of the center of the tile grid
                Vector3 cellWorldBL = parentGrid.CellToWorld(cellPos);
                Vector3 worldCenter = cellWorldBL + new Vector3(
                    nodeSize * 0.5f,
                    nodeSize * 0.5f,
                    0f
                );

                if (WorldToGrid(worldCenter, out int xIdx, out int yIdx))
                {
                    rawBlockedCells.Add(new Vector2Int(xIdx, yIdx));
                }
                else
                {
                    Debug.LogWarning($"[A*] The worldCenter is beyond the grid boundaries. WorldToGrid returns false.");
                }
            }
        }

//  If bossCollider is set, calculate the "expansion range (+1 additional margin)" based on its Collider2D type.
//    Additionally, record the offset in world coordinates as worldOffset.
int inflateRadiusInNodes = 0;
int inflateX = 0;
int inflateY = 0;
bool useCircleInflate = false;
float worldRadius = 0f;
Vector2 worldOffset = Vector2.zero;  

if (bossCollider != null)
{
    CircleCollider2D circle = bossCollider as CircleCollider2D;
    if (circle != null)
    {
        // 1) Calculate the world radius of the CircleCollider2D
        float maxScale = Mathf.Max(circle.transform.lossyScale.x, circle.transform.lossyScale.y);
        worldRadius = circle.radius * maxScale;
        // 2) Circular expansion: outward by +1 nodes
        inflateRadiusInNodes = Mathf.CeilToInt(worldRadius / nodeSize) + 1;
        useCircleInflate = true;
        Debug.Log($"[A*] CircleCollider2D, r = {worldRadius}, r2 = {inflateRadiusInNodes} (＋1)");

        // 3) Record the world offset of the center point = transform.position + offset * lossyScale
        worldOffset = new Vector2(
            circle.offset.x * circle.transform.lossyScale.x,
            circle.offset.y * circle.transform.lossyScale.y
        );
    }
    else
    {
        BoxCollider2D box = bossCollider as BoxCollider2D;
        if (box != null)
        {
            // 1) The BoxCollider2D's width and height in the world are half of the total width and height.
            Vector3 lossy = box.transform.lossyScale;
            float halfW = box.size.x * 0.5f * lossy.x;
            float halfH = box.size.y * 0.5f * lossy.y;
            // 2) Rectangular expansion: Add 1 node outward
            inflateX = Mathf.CeilToInt(halfW / nodeSize) + 1;
            inflateY = Mathf.CeilToInt(halfH / nodeSize) + 1;

            // 3) Record the world offset (add box.offset * lossyScale to it)
            worldOffset = new Vector2(
                box.offset.x * lossy.x,
                box.offset.y * lossy.y
            );
        }
        else
        {
            // Other Collider2D types: fallback uses bounds.extents for rectangular expansion
            Bounds ext = bossCollider.bounds;
            float halfW = ext.extents.x;
            float halfH = ext.extents.y;
            inflateX = Mathf.CeilToInt(halfW / nodeSize) + 1;
            inflateY = Mathf.CeilToInt(halfH / nodeSize) + 1;
            
            // worldOffset = (0,0), since we don't know the exact offset, no additional offset is applied here.
            worldOffset = Vector2.zero;
        }
    }
}

// 4. Expand each original obstacle and mark all the adjacent cells within the collision range (+1) as impassable.
foreach (Vector2Int blocked in rawBlockedCells)
{
    int bx = blocked.x;
    int by = blocked.y;

    // 1) First, calculate the position of the "original tile center point" in the world space:
    Vector3 obstacleCenter = GridToWorld(bx, by);
    // 2) Then add the worldOffset of Collider2D, and you will get the "expansion reference center":
    Vector3 inflateCenter = obstacleCenter + new Vector3(worldOffset.x, worldOffset.y, 0f);

    if (bossCollider == null)
    {
        // If there is no collider, mark each cell individually.
        walkable[bx, by] = false;
        continue;
    }

    if (useCircleInflate)
    {
        // Circular expansion: Traverse within a square area, and then inflate according to the "inflateCenter" distance.
        for (int dx = -inflateRadiusInNodes; dx <= inflateRadiusInNodes; dx++)
        {
            for (int dy = -inflateRadiusInNodes; dy <= inflateRadiusInNodes; dy++)
            {
                int nx = bx + dx;
                int ny = by + dy;
                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;

                // First, calculate the world coordinates of the center of this grid.
                Vector3 thisNodeCenter = GridToWorld(nx, ny);
                float dist = Vector3.Distance(thisNodeCenter, inflateCenter);

                // The original radius + half a grid (nodeSize * 0.5f) and then + 1 grid (due to the Ceil + 1, a little buffer is added here)
                if (dist <= worldRadius + nodeSize * 0.5f)
                {
                    walkable[nx, ny] = false;
                }
            }
        }
    }
    else
    {
        // Rectangle expansion (BoxCollider2D or other Collider2D)
// Here, we need to ensure that the "square range" is aligned with the "inflateCenter", rather than with "GridToWorld(bx,by)".
// First, calculate which grid the inflateCenter belongs to:
        if (!WorldToGrid(inflateCenter, out int centerGX, out int centerGY))
        {
            // If the inflateCenter itself has already exceeded the grid boundaries, simply mark the original obstacle point directly.
            walkable[bx, by] = false;
        }
        else
        {
            // inflateX and inflateY respectively represent the number of grid cells corresponding to the half width/half height.
            for (int dx = -inflateX; dx <= inflateX; dx++)
            {
                for (int dy = -inflateY; dy <= inflateY; dy++)
                {
                    int nx = centerGX + dx;
                    int ny = centerGY + dy;
                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                        continue;
                    walkable[nx, ny] = false;
                }
            }
        }
    }
}

        // 4. Expand each original obstacle and mark all the adjacent cells within the collision range (+1) as impassable.
        foreach (Vector2Int blocked in rawBlockedCells)
        {
            int bx = blocked.x;
            int by = blocked.y;

            if (bossCollider == null)
            {
                // Do not expand, directly mark the center square of the obstacle.
                walkable[bx, by] = false;
                continue;
            }

            if (useCircleInflate)
            {
                // Circular expansion: Traverse within the square range and then use distance to determine
                for (int dx = -inflateRadiusInNodes; dx <= inflateRadiusInNodes; dx++)
                {
                    for (int dy = -inflateRadiusInNodes; dy <= inflateRadiusInNodes; dy++)
                    {
                        int nx = bx + dx;
                        int ny = by + dy;

                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;

                        // Calculate the world distance from the center of the grid to the center of the original obstacle.
                        Vector3 thisNodeCenter = GridToWorld(nx, ny);
                        Vector3 originalCenter = GridToWorld(bx, by);
                        float dist = Vector3.Distance(thisNodeCenter, originalCenter);

                        // Even within the original radius range, keep an additional 1 unit.
                        if (dist <= worldRadius + nodeSize * 0.5f)
                        {
                            walkable[nx, ny] = false;
                        }
                    }
                }
            }
            else
            {
                // Rectangular expansion (BoxCollider2D or other Collider2D)
                for (int dx = -inflateX; dx <= inflateX; dx++)
                {
                    for (int dy = -inflateY; dy <= inflateY; dy++)
                    {
                        int nx = bx + dx;
                        int ny = by + dy;

                        if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                            continue;

                        walkable[nx, ny] = false;
                    }
                }
            }
        }


        // 5. Initialize the nodes based on the "walkable" array
        int blockedCount = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                nodes[x, y] = new Node(x, y, walkable[x, y]);
                if (!walkable[x, y]) blockedCount++;
            }
        }
    }

    /// <summary>
    /// Convert the world coordinate worldPos to the grid index (gridX, gridY). Return false if it exceeds the range.
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int gridX, out int gridY)
    {
        Vector3 local = worldPos - originPosition;
        gridX = Mathf.FloorToInt(local.x / nodeSize);
        gridY = Mathf.FloorToInt(local.y / nodeSize);
        if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
            return false;
        return true;
    }

    /// <summary>
    /// Convert the grid index (gridX, gridY) to the world coordinates of the center of that grid cell.
    /// </summary>
    public Vector3 GridToWorld(int gridX, int gridY)
    {
        return originPosition + new Vector3(
            (gridX + 0.5f) * nodeSize,
            (gridY + 0.5f) * nodeSize,
            0f
        );
    }

    /// <summary>
    /// A* Pathfinding Interface: From startWorld to endWorld, it returns a series of "grid center world coordinates" as the path. If no path is found, it returns null.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
{
   

    if (gridWidth <= 0 || gridHeight <= 0)
    {
       
        return null;
    }
    // 1. Convert world coordinates -> grid index
    if (!WorldToGrid(startWorld, out int startX, out int startY))
    {
        
        return null;
    }
    if (!WorldToGrid(endWorld, out int endX, out int endY))
    {
       
        return null;
    }

    // 2. If the "destination square" happens to be marked as an obstacle, then try to expand in all directions to find the nearest "walkable" area.
    if (!walkable[endX, endY])
    {
        
        if (!FindNearestWalkable(ref endX, ref endY))
        {
          
            return null;
        }
        Vector3 newEndWorld = GridToWorld(endX, endY);
        
        endWorld = newEndWorld;
    }

    Node startNode = nodes[startX, startY];
    Node endNode   = nodes[endX, endY];
    

    // The following is the logic of A*
    List<Node> openList = new List<Node>();
    HashSet<Node> closedSet = new HashSet<Node>();
    // Reset
    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            nodes[x, y].gCost = int.MaxValue;
            nodes[x, y].hCost = 0;
            nodes[x, y].parent = null;
        }
    }
    startNode.gCost = 0;
    startNode.hCost = GetDistance(startNode, endNode);
    openList.Add(startNode);

    while (openList.Count > 0)
    {
        Node current = openList[0];
        for (int i = 1; i < openList.Count; i++)
        {
            if (openList[i].fCost < current.fCost ||
                (openList[i].fCost == current.fCost && openList[i].hCost < current.hCost))
            {
                current = openList[i];
            }
        }
        openList.Remove(current);
        closedSet.Add(current);

        if (current == endNode)
        {
            
            return RetracePath(startNode, endNode);
        }

        foreach (Node neighbor in GetNeighbors(current))
        {
            if (!neighbor.walkable || closedSet.Contains(neighbor))
                continue;

            int newG = current.gCost + GetDistance(current, neighbor);
            if (newG < neighbor.gCost)
            {
                neighbor.gCost = newG;
                neighbor.hCost = GetDistance(neighbor, endNode);
                neighbor.parent = current;

                if (!openList.Contains(neighbor))
                    openList.Add(neighbor);
            }
        }
    }


    return null;
}

/// <summary>
/// If (gridX, gridY) is not walkable, start a BFS/diffusion from it to find the "closest walkable" cell.
/// Before calling, ensure that gridX and gridY are within the range [0, gridWidth) × [0, gridHeight).
/// If found, update gridX and gridY to the new and closest walkable cell and return true; otherwise, return false.
/// </summary>
private bool FindNearestWalkable(ref int gridX, ref int gridY)
{
    if (walkable[gridX, gridY]) return true;

    // Set of diffusion step lengths in four directions (Manhattan distance)
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    bool[,] visited = new bool[gridWidth, gridHeight];
    queue.Enqueue(new Vector2Int(gridX, gridY));
    visited[gridX, gridY] = true;

    int[,] dirs = new int[,] { { 1,0 },{ -1,0 },{ 0,1 },{ 0,-1 } };
    while (queue.Count > 0)
    {
        Vector2Int cur = queue.Dequeue();
        for (int i = 0; i < 4; i++)
        {
            int nx = cur.x + dirs[i, 0];
            int ny = cur.y + dirs[i, 1];
            if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight) 
                continue;
            if (visited[nx, ny]) 
                continue;

            // Once a "walkable = true" is found, simply return.
            if (walkable[nx, ny])
            {
                gridX = nx;
                gridY = ny;
                return true;
            }
            visited[nx, ny] = true;
            queue.Enqueue(new Vector2Int(nx, ny));
        }
    }

    return false;
}

    /// <summary>
    /// Traverse the node chain from the starting point to the ending point and convert it into a list of world coordinates.
    /// </summary>
    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node current = endNode;
        while (current != startNode)
        {
            path.Add(GridToWorld(current.x, current.y));
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Obtain the four neighboring nodes in the four directions of the current node
    /// </summary>
    private List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
        for (int i = 0; i < dirs.GetLength(0); i++)
        {
            int nx = node.x + dirs[i, 0];
            int ny = node.y + dirs[i, 1];
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
            {
                neighbors.Add(nodes[nx, ny]);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// Calculate the Manhattan distance between two nodes
    /// </summary>
    private int GetDistance(Node a, Node b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
