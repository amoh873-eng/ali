SELECT u.Email, STRING_AGG(r.Name, ',') WITHIN GROUP (ORDER BY r.Name) AS Roles
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON ur.UserId = u.Id
LEFT JOIN AspNetRoles r ON r.Id = ur.RoleId
GROUP BY u.Email
ORDER BY u.Email;