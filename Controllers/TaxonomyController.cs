using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.DTOs;
using Zootify_Backend.Enums;
using Zootify_Backend.Models;

namespace Zootify_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxonomyController(ZootifyDbContext db) : ControllerBase
{
    // GET /api/Taxonomy — alle Knoten flach zurückgeben
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetAll()
    {
        var nodes = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .ToListAsync();

        return Ok(nodes.Select(t => MapToDto(t, includeChildren: false)));
    }

    // GET /api/Taxonomy/tree — gesamten Baum ab Wurzel zurückgeben
    [HttpGet("tree")]
    public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetTree()
    {
        var allNodes = await db.TaxonomyNodes
            .Include(t => t.Children)
            .ToListAsync();

        var roots = allNodes
            .Where(t => t.ParentId == null)
            .Select(t => MapToDto(t, includeChildren: true, allNodes))
            .ToList();

        return Ok(roots);
    }

    // GET /api/Taxonomy/{id} — einzelnen Knoten mit Kindern zurückgeben
    [HttpGet("{id}")]
    public async Task<ActionResult<TaxonomyNodeDto>> GetById(int id)
    {
        var node = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (node is null)
            return NotFound();

        return Ok(MapToDto(node, includeChildren: true));
    }

    // GET /api/Taxonomy/rank/{rank} — alle Knoten eines bestimmten Rangs
    [HttpGet("rank/{rank}")]
    public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetByRank(TaxonomyRank rank)
    {
        var nodes = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .Where(t => t.Rank == rank && t.ApprovalStatus == ApprovalStatus.Approved)
            .ToListAsync();

        return Ok(nodes.Select(t => MapToDto(t, includeChildren: false)));
    }

    // GET /api/Taxonomy/pending — alle Vorschläge die auf Genehmigung warten
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<TaxonomyNodeDto>>> GetPending()
    {
        var nodes = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .Where(t => t.ApprovalStatus == ApprovalStatus.Pending)
            .ToListAsync();

        return Ok(nodes.Select(t => MapToDto(t, includeChildren: false)));
    }

    // POST /api/Taxonomy — neuen Knoten vorschlagen (startet als Pending)
    [HttpPost]
    public async Task<ActionResult<TaxonomyNodeDto>> Create(CreateTaxonomyNodeDto dto)
    {
        if (dto.ParentId.HasValue && !await db.TaxonomyNodes.AnyAsync(t => t.Id == dto.ParentId))
            return BadRequest("Elternknoten nicht gefunden.");

        var node = new TaxonomyNode
        {
            Name             = dto.Name,
            Rank             = dto.Rank,
            ParentId         = dto.ParentId,
            ProposedByUserId = dto.ProposedByUserId,
            ApprovalStatus   = ApprovalStatus.Pending
        };

        db.TaxonomyNodes.Add(node);
        await db.SaveChangesAsync();

        await db.Entry(node).Reference(t => t.Parent).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = node.Id }, MapToDto(node, includeChildren: false));
    }

    // PUT /api/Taxonomy/{id}/approve — Vorschlag genehmigen
    [HttpPut("{id}/approve")]
    public async Task<ActionResult<TaxonomyNodeDto>> Approve(int id)
    {
        var node = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (node is null)
            return NotFound();

        node.ApprovalStatus = ApprovalStatus.Approved;
        await db.SaveChangesAsync();

        return Ok(MapToDto(node, includeChildren: false));
    }

    // PUT /api/Taxonomy/{id}/reject — Vorschlag ablehnen
    [HttpPut("{id}/reject")]
    public async Task<ActionResult<TaxonomyNodeDto>> Reject(int id)
    {
        var node = await db.TaxonomyNodes
            .Include(t => t.Parent)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (node is null)
            return NotFound();

        node.ApprovalStatus = ApprovalStatus.Rejected;
        await db.SaveChangesAsync();

        return Ok(MapToDto(node, includeChildren: false));
    }

    // DELETE /api/Taxonomy/{id} — Knoten löschen (nur wenn keine Kinder)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var node = await db.TaxonomyNodes
            .Include(t => t.Children)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (node is null)
            return NotFound();

        if (node.Children.Count > 0)
            return Conflict("Knoten hat noch Unterknoten und kann nicht gelöscht werden.");

        db.TaxonomyNodes.Remove(node);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // Wandelt ein TaxonomyNode-Model in ein DTO um.
    // includeChildren: true  → Kinder rekursiv mit einbauen (für Baum-Endpunkte)
    // includeChildren: false → nur der Knoten selbst, ohne Kinder (für flache Listen)
    // allNodes: alle Knoten aus der DB — wenn übergeben werden Kinder direkt aus der Liste geholt statt erneut die DB zu fragen
    private static TaxonomyNodeDto MapToDto(TaxonomyNode t, bool includeChildren, List<TaxonomyNode>? allNodes = null)
    {
        var dto = new TaxonomyNodeDto
        {
            Id               = t.Id,
            Name             = t.Name,
            Rank             = t.Rank,
            ApprovalStatus   = t.ApprovalStatus,
            ParentId         = t.ParentId,
            ParentName       = t.Parent?.Name,
            ProposedByUserId = t.ProposedByUserId
        };

        if (includeChildren)
        {
            var children = allNodes != null
                ? allNodes.Where(n => n.ParentId == t.Id).ToList()
                : t.Children.ToList();

            dto.Children = children
                .Select(c => MapToDto(c, includeChildren: true, allNodes))
                .ToList();
        }

        return dto;
    }
}
