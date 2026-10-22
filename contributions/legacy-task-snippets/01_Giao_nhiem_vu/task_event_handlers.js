/*
 * SOURCE EXCERPT — not standalone runnable code
 * Original path: frontend/sprint2-ui.js
 * Original lines: [(397, 401)]
 * Shared helpers/framework remain in source project.
 */

      else if (key === "task-new" && (can("tasks.manage") || can("assignments.manage"))) taskForm();
      else if (key === "task-edit" && role("Mentor") && can("tasks.manage")) {
        const task = data.mentorTasks?.find(x => String(x.id) === String(id));
        if (task) taskForm(task);
      }
